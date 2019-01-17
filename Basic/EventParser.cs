using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

    public interface IEventPoolMgr
    {
        void Remove(EventCallType func);
        void Add(EventCallType func);
        EventCallType Get(string funcName);
    }

    public class EventParser : EventParserBase
    {
        static EventParser instance;
        public static EventParser Instance
        {
            get
            {
                if (instance == null)
                    instance = new EventParser();
                return instance;
            }
        }
    }

    public class EventParserBase
    {
        protected char[] splitChars = { '(', ')', '{', '}', ',', ' ', '\t' };
        protected IEventPoolMgr poolType = null;

        public void Init(IEventPoolMgr t)
        {
            poolType = t;
        }

        public EventCall Parse(string eventsString)
        {
            try
            {
                Queue<string> stringQueue = GetStringsList(eventsString);
                return Parse(stringQueue);
            }
            catch (System.Exception ex)
            {
                LOG.Erro("Parse Events Error:" + eventsString + ", Cause: " + ex);
                return null;
            }
        }

        public EventCall GetFunc(string funcName, object[] parameters)
        {
            // 解析方法
            EventCallType method = poolType.Get(funcName);
            if (method == null)
                return null;

            return new EventCall(method, parameters);
        }

        //--------------------------------------------------------------------
        #region 内部方法
        EventCall Parse(Queue<string> stringQueue)
        {
            string word = null;
            EventCall first = null, prev = null, last = null;
            while (stringQueue.Count > 0)
            {
                word = stringQueue.Dequeue();
                if (word == "(")
                {
                    last = Parse(stringQueue);
                }
                else
                {
                    last = ParseFunc(word, stringQueue);
                }

                if (last == null)
                    continue;

                if (first == null)
                {
                    first = prev = last;
                }
                else
                {
                    prev.NextCall = last;
                    prev = last;
                }
                last = null;
            }
            return first;
        }

        Queue<string> GetStringsList(string eventsString)
        {
            Queue<string> stringQueue = new Queue<string>();
            if (eventsString == null || eventsString.Length <= 0)
                return stringQueue;

            int index = 0;
            int startIndex = 0;
            string subStr = null;
            char startChar = eventsString[startIndex];
            while (true)
            {
                if (startChar == '"' || startChar == '\'')
                {
                    if ((index = eventsString.IndexOf(startChar, startIndex + 1)) == -1)
                        break;
                    index += 1;

                    int subStrLen = index - startIndex;
                    if (subStrLen > 0)
                    {
                        subStr = eventsString.Substring(startIndex, subStrLen).Trim();
                        if (subStr.Length > 0)
                            stringQueue.Enqueue(subStr);
                    }
                    startIndex = index;
                    if (startIndex >= eventsString.Length)
                        break;
                    startChar = eventsString[startIndex];
                }
                else
                {
                    if ((index = eventsString.IndexOfAny(splitChars, startIndex)) == -1)
                        break;

                    int subStrLen = index - startIndex;
                    if (subStrLen > 0)
                    {
                        subStr = eventsString.Substring(startIndex, subStrLen).Trim();
                        if (subStr.Length > 0)
                            stringQueue.Enqueue(subStr);
                    }
                    startIndex = index;
                    startChar = eventsString[index];
                    if(startChar != '"' && startChar != '\'')
                    {
                        // 添加分隔符(空格除外)
                        subStr = eventsString.Substring(index, 1).Trim();
                        if (subStr.Length > 0)
                            stringQueue.Enqueue(subStr);
                        startIndex = index + 1;

                        if (startIndex >= eventsString.Length)
                            break;
                        startChar = eventsString[startIndex];
                    }
                }
            }

            // 添加最后一个串
            if (startIndex < eventsString.Length)
            {
                subStr = eventsString.Substring(startIndex, eventsString.Length - startIndex).Trim();
                if (subStr.Length > 0)
                    stringQueue.Enqueue(subStr);
            }
            return stringQueue;
        }

        protected string ParseString(string paramString, ref int startIndex, char bracketChar)
        {
            if (paramString[startIndex] != bracketChar)
                return null;

            startIndex++;
            int strEnd = paramString.IndexOf(bracketChar, startIndex);
            if (strEnd == -1)
            {
                Console.WriteLine("参数格式错误:{0},{1}", paramString, startIndex);
                return null;
            }
            string strParam = paramString.Substring(startIndex, strEnd - startIndex);
            startIndex = ++strEnd;
            return strParam;
        }

        protected List<object> ParseParams(Queue<string> stringQueue, char endBracket)
        {
            string word = null;
            List<object> parameters = new List<object>();
            List<object> subParams = null;
            bool exprComplete = false;
            while (stringQueue.Count > 0)
            {
                word = stringQueue.Dequeue();
                if (word[0] == ',')
                    break;
                else if (word[0] == endBracket)
                {
                    exprComplete = true;
                    break;
                }
                else if (word[0] == '\'')
                {
                    int endPos = word.LastIndexOf('\'');
                    if (endPos > 0)
                        parameters.Add(word.Substring(1, endPos - 1));
                    else
                        break;
                }
                else if (word[0] == '"')
                {
                    int endPos = word.LastIndexOf('"');
                    if (endPos > 0)
                        parameters.Add(word.Substring(1, endPos - 1));
                    else
                        break;
                }
                else if (word[0] == '{')
                {
                    subParams = ParseParams(stringQueue, '}');
                    if (subParams != null)
                        parameters.Add(subParams);
                    else
                        break;
                }
                else if (word == "true")
                {
                    parameters.Add(true);
                }
                else if (word == "false")
                {
                    parameters.Add(false);
                }
                else if (word[0] == '@' && word[1] == 'B')
                {
                    string str = word.Substring(2);
                    parameters.Add(int.Parse(str));
                }
                else if (word.Contains("."))
                {
                    parameters.Add(float.Parse(word));
                }
                else
                {
                    parameters.Add(int.Parse(word));
                }

                // 分隔符
                word = stringQueue.Dequeue();
                if (word[0] == ',')
                    continue;

                // 结束符
                if (word[0] == endBracket)
                    exprComplete = true;
                break;
            }

            if (!exprComplete)
            {
                Console.WriteLine("字串格式不正确:{0}", word);
                return null;
            }
            return parameters;
        }

        protected virtual EventCall ParseFunc(string funcName, Queue<string> stringQueue)
        {
            // 解析方法
            EventCallType method = poolType.Get(funcName);
            if (method == null)
                return null;

            // 解析参数
            string word = stringQueue.Dequeue();
            if (word != "(")
                return null;
            List<object> parameters = ParseParams(stringQueue, ')');
            return new EventCall(method, parameters.ToArray());
        }
        #endregion
    }