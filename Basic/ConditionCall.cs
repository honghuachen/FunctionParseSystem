using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

    public delegate double ExprCall(EventParams envParams,params double[] parametors);
    public delegate double ConnectHandler(double a, ExprBase nextExpr, EventParams envParams);

    public class ExprConnectors
    {
        public static double Add(double a, ExprBase nextExpr, EventParams envParams)
        {
            try
            {
                return a + nextExpr.Execute(envParams);
            }
            catch
            {
                Console.WriteLine("参与计算的数据类型不匹配");
                return 0;
            }
        }

        public static double Dec(double a, ExprBase nextExpr, EventParams envParams)
        {
            try
            {
                return a - nextExpr.Execute(envParams);
            }
            catch
            {
                Console.WriteLine("参与计算的数据类型不匹配");
                return 0;
            }
        }

        public static double Multiply(double a, ExprBase nextExpr, EventParams envParams)
        {
            try
            {
                return a * nextExpr.Execute(envParams);
            }
            catch
            {
                Console.WriteLine("参与计算的数据类型不匹配");
                return 0;
            }
        }

        public static double Divide(double a, ExprBase nextExpr, EventParams envParams)
        {
            try
            {
                return a / nextExpr.Execute(envParams);
            }
            catch
            {
                Console.WriteLine("参与计算的数据类型不匹配");
                return 0;
            }
        }

        public static double And(double a, ExprBase nextExpr, EventParams envParams)
        {
            try
            {
                if (!Convert.ToBoolean(a))
                    return 0;
                if (!Convert.ToBoolean(nextExpr.Execute(envParams)))
                    return 0;
                return 1;
            }
            catch
            {
                Console.WriteLine("参与计算的数据类型不匹配");
                return 0;
            }
        }
        public static double Or(double a, ExprBase nextExpr, EventParams envParams)
        {
            try
            {
                if (Convert.ToBoolean(a))
                    return 1;
                if (Convert.ToBoolean(nextExpr.Execute(envParams)))
                    return 1;
                return 0;
            }
            catch
            {
                Console.WriteLine("参与计算的数据类型不匹配");
                return 0;
            }
        }

        public static double Less(double a, ExprBase nextExpr, EventParams envParams)
        {
            try
            {
                return (a < nextExpr.Execute(envParams)) ? 1 : 0;
            }
            catch
            {
                Console.WriteLine("参与计算的数据类型不匹配");
                return 0;
            }
        }

        public static double LessEqual(double a, ExprBase nextExpr, EventParams envParams)
        {
            try
            {
                return (a <= nextExpr.Execute(envParams)) ? 1 : 0;
            }
            catch
            {
                Console.WriteLine("参与计算的数据类型不匹配");
                return 0;
            }
        }

        public static double Great(double a, ExprBase nextExpr, EventParams envParams)
        {
            try
            {
                return (a > nextExpr.Execute(envParams)) ? 1 : 0;
            }
            catch
            {
                Console.WriteLine("参与计算的数据类型不匹配");
                return 0;
            }
        }

        public static double GreatEqual(double a, ExprBase nextExpr, EventParams envParams)
        {
            try
            {
                return (a >= nextExpr.Execute(envParams)) ? 1 : 0;
            }
            catch
            {
                Console.WriteLine("参与计算的数据类型不匹配");
                return 0;
            }
        }

        public static double Equal(double a, ExprBase nextExpr, EventParams envParams)
        {
            try
            {
                return (a == nextExpr.Execute(envParams)) ? 1 : 0;
            }
            catch
            {
                Console.WriteLine("参与计算的数据类型不匹配");
                return 0;
            }
        }

        public static double NoEqual(double a, ExprBase nextExpr, EventParams envParams)
        {
            try
            {
                return (a != nextExpr.Execute(envParams)) ? 1 : 0;
            }
            catch
            {
                Console.WriteLine("参与计算的数据类型不匹配");
                return 0;
            }
        }

        static Dictionary<string, ConnectHandler> connectors = new Dictionary<string, ConnectHandler>();

        static ExprConnectors()
        {
            connectors.Add("and", new ConnectHandler(And));
            connectors.Add("or", new ConnectHandler(Or));

            connectors.Add("<", new ConnectHandler(Less));
            connectors.Add(">", new ConnectHandler(Great));
            connectors.Add("<=", new ConnectHandler(LessEqual));
            connectors.Add(">=", new ConnectHandler(GreatEqual));
            connectors.Add("==", new ConnectHandler(Equal));
            connectors.Add("!=", new ConnectHandler(NoEqual));

            connectors.Add("+", new ConnectHandler(Add));
            connectors.Add("-", new ConnectHandler(Dec));
            connectors.Add("*", new ConnectHandler(Multiply));
            connectors.Add("/", new ConnectHandler(Divide));
        }

        public static ConnectHandler Get(string connectStr)
        {
            ConnectHandler ret = null;
            connectors.TryGetValue(connectStr, out ret);
            return ret;
        }
    }
    public class ExprBase
    {
        public ConnectHandler connector;
        public ExprBase next;
        protected virtual double CalcResult(EventParams runParams)
        {
            return 0;
        }

        public double Execute(EventParams runParams)
        {
            double result = CalcResult(runParams);
            if (next != null)
            {
                if (connector != null)
                    result = connector(result, next, runParams);
            }
            return result;
        }

        public bool Execute() {
            return Convert.ToBoolean(Execute(null));
        }
    }

    public class SubExpr : ExprBase
    {
        ExprBase val;
        public SubExpr(ExprBase val)
        {
            this.val = val;
        }
        protected override double CalcResult(EventParams runParams)
        {
            return val.Execute(runParams);
        }
    }

    public class StrExpr : ExprBase
    {
        static Map<int,string> valMap = new Map<int,string>();
        public static string Get(int strHashCode)
        {
            string ret = null;
            if (!valMap.TryGetValue(strHashCode, out ret))
                return "";
            return ret;
        }

        int strHashCode;
        public StrExpr(string val)
        {
            this.strHashCode = val.GetHashCode();
            valMap[strHashCode] = val;
        }
        protected override double CalcResult(EventParams runParams)
        {
            return strHashCode;
        }
    }

    public class ConstExpr : ExprBase
    {
        double val;
        public ConstExpr(double val)
        {
            this.val = val;
        }
        protected override double CalcResult(EventParams runParams)
        {
            return val;
        }
    }

    public class ListExpr : ExprBase
    {
        List<ExprBase> exprs;
        double[] result;

        public ListExpr(List<ExprBase> exprs)
        {
            this.exprs = exprs;
            result = new double[exprs.Count];
        }
        public double[] CalcListResult(EventParams runParams)
        {
            for (int i = 0; i < exprs.Count; i++)
            {
                result[i] = exprs[i].Execute(runParams);
            }
            return result;
        }
    }

    public class FuncExpr : ExprBase {
        ExprCall method;
        ListExpr exprList;

        public FuncExpr(ExprCall method, List<ExprBase> exprList) {
            this.method = method;
            this.exprList = new ListExpr(exprList);
        }

        protected override double CalcResult(EventParams runParams) {
            return method(runParams,exprList.CalcListResult(runParams));
        }
    }