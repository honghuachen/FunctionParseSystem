using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Collections;
using UnityEngine;

    public interface IConditionPool
    {
        void Remove(ExprCall func);
        void Add(ExprCall func);
        ExprCall Get(string funcName);
    }

    public class ConditionParser : ConditionParserBase
    {
        static ConditionParser instance;
        public static ConditionParser Instance
        {
            get
            {
                if (instance == null)
                    instance = new ConditionParser();
                return instance;
            }
        }
    }

    public class ConditionParserBase {
        char[] splitChars = { '(', ')', '{', '}', ',', ' ', '\t', '>', '<', '=', '!', };
        IConditionPool exprPool = null;

        public void Init(IConditionPool t) {
            exprPool = t;
        }

        public ExprBase Parse(string conditionsString) {
            try {
                Queue<string> stringQueue = GetStringsList(conditionsString);
                return Parse(stringQueue);
            } catch (System.Exception ex) {
                LOG.Erro("Parse Conditions Error:" + conditionsString + ", Cause: " + ex);
                return null;
            }
        }

        //--------------------------------------------------------------------
        #region 内部方法
        ExprBase Parse(Queue<string> stringQueue) {
            string word = null;
            ExprBase first = null, prev = null, last = null;
            while (stringQueue.Count > 0) {
                word = stringQueue.Dequeue();
                if (word == "(") {
                    ExprBase subExpr = Parse(stringQueue);
                    if (subExpr == null)
                        return null;
                    last = new SubExpr(subExpr);
                } else if (word[0] == '\'') {
                    int endPos = word.LastIndexOf('\'');
                    if (endPos > 0)
                        last = new StrExpr(word.Substring(1, endPos - 1));
                    else
                        return null;
                } else if (word[0] == '\"') {
                    int endPos = word.LastIndexOf('\"');
                    if (endPos > 0)
                        last = new StrExpr(word.Substring(1, endPos - 1));
                    else
                        return null;
                } else if (word == "true") {
                    last = new ConstExpr(1);
                } else if (word == "false") {
                    last = new ConstExpr(0);
                } else if (word[0] == '-' || Char.IsDigit(word[0])) {
                    if (word.Contains(".")) {
                        last = new ConstExpr(float.Parse(word));
                    } else {
                        last = new ConstExpr(int.Parse(word));
                    }
                } else {
                    last = ParseFunc(word, stringQueue);
                }

                if (last == null)
                    return null;

                if (first == null) {
                    first = prev = last;
                } else {
                    prev.next = last;
                    prev = last;
                }

                // 条件连接关系
                if (stringQueue.Count > 0) {
                    word = stringQueue.Dequeue();
                    if (word == ")")
                        break;

                    last.connector = ExprConnectors.Get(word);
                    if (last.connector == null)
                        return null;
                }

                last = null;
            }
            return first;
        }

        Queue<string> GetStringsList(string eventsString) {
            Queue<string> stringQueue = new Queue<string>();
            if (eventsString == null)
                return stringQueue;

            int index = 0;
            int startIndex = 0;
            string subStr = null;
            while ((index = eventsString.IndexOfAny(splitChars, startIndex)) != -1) {
                int subStrLen = index - startIndex;
                if (subStrLen > 0) {
                    subStr = eventsString.Substring(startIndex, subStrLen).Trim();
                    if (subStr.Length > 0)
                        stringQueue.Enqueue(subStr);
                }

                // 添加分隔符(空格除外)
                int spliterLen = 1;
                if (eventsString.Length > index + 1) {
                    switch (eventsString[index]) {
                    case '<':
                        if (eventsString[index + 1] == '=')
                            subStr = "<=";
                        else
                            subStr = "<";
                        break;
                    case '>':
                        if (eventsString[index + 1] == '=')
                            subStr = ">=";
                        else
                            subStr = ">";
                        break;
                    case '=':
                        if (eventsString[index + 1] == '=')
                            subStr = "==";
                        else {
                            throw new Exception("语法错误:'=',表达式原文:" + eventsString);
                        }
                        break;
                    case '!':
                        if (eventsString[index + 1] == '=')
                            subStr = "!=";
                        else {
                            throw new Exception("语法错误:'!',表达式原文:" + eventsString);
                        }
                        break;
                    default:
                        subStr = eventsString.Substring(index, 1);
                        break;
                    }
                    spliterLen = subStr.Length;
                    subStr = subStr.Trim();
                } else {
                    subStr = eventsString.Substring(index, 1).Trim();
                }

                if (subStr.Length > 0)
                    stringQueue.Enqueue(subStr);
                startIndex = index + spliterLen;
            }

            // 添加最后一个串
            if (startIndex < eventsString.Length) {
                subStr = eventsString.Substring(startIndex, eventsString.Length - startIndex).Trim();
                if (subStr.Length > 0)
                    stringQueue.Enqueue(subStr);
            }
            return stringQueue;
        }

        List<ExprBase> ParseParams(Queue<string> stringQueue, char endBracket) {
            string word = null;
            List<ExprBase> parameters = new List<ExprBase>();
            List<ExprBase> subParams = null;
            bool exprComplete = false;
            while (stringQueue.Count > 0) {
                word = stringQueue.Dequeue();
                if (word[0] == ',')
                    break;
                else if (word[0] == endBracket) {
                    exprComplete = true;
                    break;
                } else if (word[0] == '\'') {
                    int endPos = word.LastIndexOf('\'');
                    if (endPos < 0)
                        break;
                    parameters.Add(new StrExpr(word.Substring(1, endPos - 1)));
                } else if (word[0] == '"') {
                    int endPos = word.LastIndexOf('"');
                    if (endPos < 0)
                        break;
                    parameters.Add(new StrExpr(word.Substring(1, endPos - 1)));
                } else if (word[0] == '{') {
                    subParams = ParseParams(stringQueue, '}');
                    if (subParams == null)
                        break;
                    parameters.Add(new ListExpr(subParams));
                } else if (word == "true") {
                    parameters.Add(new ConstExpr(1));
                } else if (word == "false") {
                    parameters.Add(new ConstExpr(0));
                } else if (word[0] == '@' && word[1] == 'B') {
                    string str = word.Substring(2);
                    parameters.Add(new ConstExpr(int.Parse(str)));
                } else if (Char.IsDigit(word[0]) || word[0] == '-') {
                    if (word.Contains(".")) {
                        parameters.Add(new ConstExpr(float.Parse(word)));
                    } else {
                        parameters.Add(new ConstExpr(int.Parse(word)));
                    }
                } else {
                    ExprBase funcExpr = ParseFunc(word, stringQueue);
                    if (funcExpr == null)
                        break;
                    parameters.Add(funcExpr);
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

            if (!exprComplete) {
                Console.WriteLine("字串格式不正确:{0}", word);
                return null;
            }
            return parameters;
        }

        ExprBase ParseFunc(string funcName, Queue<string> stringQueue) {
            // 解析方法
            ExprCall method = exprPool.Get(funcName);
            if (method == null)
                return null;

            // 解析参数
            string word = stringQueue.Dequeue();
            if (word != "(")
                return null;
            List<ExprBase> parameters = ParseParams(stringQueue, ')');
            return new FuncExpr(method, parameters);
        }
        #endregion
    }
