#if (!ILRUNTIME && !REFLECTION_HOTFIX) || HOTFIXDLL

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class ExpressionParser
{
    private Dictionary<string, Func<object[], object>> FuncDic = new Dictionary<string, Func<object[], object>>();
    private Dictionary<string, object> ValueDic = new Dictionary<string, object>();

    private List<string> Interrupt = new List<string>
    {
        "(",
        ",",
        "*",
        "/",
        "+",
        "-",
        "?",
        ">=",
        ">",
        "<=",
        "<",
        "==",
        "&&",
        "||",
        "!",
        "!="
    };

    private List<string> DoubleSign = new List<string>
    {
        ">=",
        ">",
        "<=",
        "<",
        "==",
        "&&",
        "||",
        "!",
        "!="
    };

    /*裁切掉Math.部分的正则*/
    private string CutMathRegExp = @"Math\.";

    /**使用公式运行
    * 由于该套解释不支持"."运算，所有Math.使用时被去掉。
    * 如：Math.min(1,2)=>min(1,2),相同的结果：1。
    * @param expression 基础数据表达式.
    *
    * 例子：
    * var compiler:my9yu.MathParser=new my9yu.MathParser();
    * //表达式的参数:键值对
    * compiler.addParams({n1:100});
    * //基础数据表达式
    * compiler.runExpression("Math.ceil(n1/250)")
    */
    public object runExpression(string expression)
    {
        if (expression != null)
        {
            MatchCollection mc = Regex.Matches(expression, CutMathRegExp);
            if (mc.Count > 0)
            {
                expression = expression.Replace(this.CutMathRegExp, "");
            }

            return this.run(expression);
        }

        return false;
    }

    /**
	* 运行脚本并得到运行结果
	* @param expression
	* @return
	*/
    public object run(string expression, string id = "", string cond = "End")
    {
        //UnityEngine.Profiling.Profiler.BeginSample("[ExpressionParser " + expression + "]");
        if (expression == null)
            return false;

        if (expression.IndexOf(" ") != -1)
        {
            //去掉所有空格
            expression = expression.Replace(" ", "");
        }

        int index = expression.IndexOf(")");

        while (index != -1)
        {
            int rightIndex = expression.IndexOf(")");
            int leftIndex = expression.Substring(0, rightIndex).LastIndexOf("(");
            int endLen = rightIndex - leftIndex - 1;

            string group = expression.Substring(leftIndex + 1, endLen);

            string newExpr = "";
            if (leftIndex != 0) newExpr += expression.Substring(0, leftIndex);

            object expVal = this.Validate(group);
            /*
            int len = newExpr.Length - 1;
            string fnName = "";
            int count = 0;
            for (int i = len; i >= 0; i--)
            {
                string s = newExpr.Substring(i, 1);
                // 过滤空格 
                if (s == " ")
                {
                    count++;
                    continue;
                }
                if (s == "<" || s == ">" || s == "|" || s == "&" || s == "=")
                {
                    string seach = newExpr.Substring(i - 1, 1);
                    if (this.Interrupt.IndexOf(seach + s) != -1)
                    {
                        s = seach + s;
                        i = i - 1;
                    }
                }
                if (this.Interrupt.IndexOf(s) != -1)
                {
                    break;
                }
                fnName = s + fnName;
                count++;
            }
            */
            string fnName = this.GetLeftName(newExpr);
            if (fnName.Length > 0)
            {
                Func<object[], object> fn = null;
                this.FuncDic.TryGetValue(fnName, out fn);
                if (fn != null)
                {
                    string parStr = expVal.ToString();
                    if (parStr.IndexOf(",") > 0)
                    {
                        //有多个参数
                        string[] pars = parStr.Split(',');
                        expVal = fn(pars);
                    }
                    else
                    {
                        //只有一个参数
                        expVal = fn(new object[] { parStr });
                    }

                }
                else
                {
                    Debug.LogError("ExpressionParser, 表达式找不到对应的函数：" + fnName);
                    return null;
                    //fn = Math[fnName];
                    //expVal = fn.apply(null, pars);
                }

                if (expVal == null || expVal is GameObject || expVal is Vector3)
                {
                    //查找到显示对象，特殊处理
                    return expVal;
                }

                newExpr = newExpr.Substring(0, newExpr.Length - fnName.Length);
            }

            newExpr += expVal;
            if (rightIndex != expression.Length)
            {
                newExpr += expression.Substring(rightIndex + 1, expression.Length - (rightIndex + 1));
            }
            expression = newExpr;
            index = expression.IndexOf(")");
        }

        object result = this.Validate(expression);
        //UnityEngine.Profiling.Profiler.EndSample();
#if GUIDELOG
        if (!string.IsNullOrEmpty(id))
        {
            Debug.Log(string.Format("条件检测结果  ID:{0}    cond:  {1}    result:    {2}", id, cond, expression));
        }
#endif
        return result;
    }

    /**
	* 批量的加变量，方便和原来的解析器接品一致。
	* @param param {变量名:变量值}
	*/
    public void AddParams(Dictionary<string, object> param)
    {
        foreach (KeyValuePair<string, object> pair in param)
        {
            this.ValueDic[pair.Key] = pair.Value;
        }
    }

    /**
	* 批量移变量
	* @param param
	*/
    public void RemoveParams(Dictionary<string, object> param)
    {
        foreach (KeyValuePair<string, object> pair in param)
        {
            if (this.ValueDic.ContainsKey(pair.Key))
            {
                this.ValueDic.Remove(pair.Key);
            }
        }
    }

    /**
     * 添加一个变量
     * @param variableName 变量名
     * @param value 变量值
     */
    public void AddVariable(string valueName, object value)
    {
        this.ValueDic[valueName] = value;
    }

    /**
    * 取变量值
    * @param variableName 变量名
    */
    public object GetVariable(string valueName)
    {
        if (this.ValueDic.ContainsKey(valueName))
        {
            return this.ValueDic[valueName];
        }

        return null;
    }

    /**
        * 删除指定变量
        * @param variableName 要删除的变量名
        * 
        */
    public void RemoveVariable(string valueName)
    {
        if (this.ValueDic.ContainsKey(valueName))
        {
            this.ValueDic.Remove(valueName);
        }
    }

    /**
    * 添加一个函数
    * @param functionName 函数名
    * @param fn 脚本运行到此函数时实际调用的处理函数。
    */
    public void AddFunction(string functionName, Func<object[], object> fn)
    {
        this.FuncDic[functionName] = fn;
    }

    /**
	* 删除指定函数
	* @param functionName 删除的函数对应的函数名
	*/
    public void removeFunction(string functionName)
    {
        if (this.FuncDic.ContainsKey(functionName))
        {
            this.FuncDic.Remove(functionName);
        }
    }


    private object Validate(string expression)
    {
        if (expression.IndexOf("\"") >= 0)
        {
            // 为了性能不用正则
            expression = expression.Replace("\"", "");
            expression = expression.Replace("\"", "");
            return expression;
        }
        if (expression.IndexOf("'") >= 0)
        {
            expression = expression.Replace("'", "");
            expression = expression.Replace("'", "");
            return expression;
        }
        if (expression.IndexOf(",") >= 0)
        {
            return expression;
        }

        int questionIndex = expression.IndexOf("?");
        if (questionIndex >= 0)
        {
            //三元运算
            string majorExp = expression.Substring(0, questionIndex);
            bool majorResult = (bool)this.Validate(majorExp);
            string minorExp = expression.Substring(questionIndex + 1, expression.Length - questionIndex - 1);
            string[] splitList = minorExp.Split(':');
            string group = majorResult ? splitList[0] : splitList[1];

            return this.Validate(group);
        }

        expression = this.Deal(expression, "!");


        while (true)
        {
            int selectIndex = 9999;
            string selectSign = null;

            foreach (string sign in this.Interrupt)
            {
                //优先处理最左侧的表达式
                int index = expression.IndexOf(sign);
                if (index >= 0)
                {
                    if (sign == "+" || sign == "-")
                    {
                        //处理加减时，如果表达式中有乘或除，则先不处理
                        if (index == 0)
                            break;
                        if (expression.IndexOf("*") >= 0)
                            break;

                        if (expression.IndexOf("/") >= 0)
                            break;
                    }
                    else if (sign == "&&" || sign == "||")
                    {
                        //处理并或运算时，如果表达式中有其他逻辑运算符，则先不处理

                        if (expression.IndexOf(">") >= 0)
                            break;

                        if (expression.IndexOf(">=") >= 0)
                            break;

                        if (expression.IndexOf("<") >= 0)
                            break;

                        if (expression.IndexOf("<=") >= 0)
                            break;

                        if (expression.IndexOf("==") >= 0)
                            break;
                    }

                    if (index < selectIndex)
                    {
                        selectIndex = index;
                        selectSign = sign;
                    }
                }
            }
            if (selectSign == null)
            {
                break;
            }

            expression = this.Deal(expression, selectSign);
        }

        return this.GetValue(expression);
    }


    private string GetLeftName(string expr)
    {
        int len = expr.Length - 1;
        string fnName = "";
        int count = 0;
        for (int i = len; i >= 0; i--)
        {
            string s = expr.Substring(i, 1);
            if (s == "<" || s == ">" || s == "|" || s == "&" || s == "=")
            {
                if (this.Interrupt.IndexOf(expr.Substring(i - 1, 1) + s) != -1)
                {
                    s = expr.Substring(i - 1, 1) + s;
                    i = i - 1;
                }
            }
            if (this.Interrupt.IndexOf(s) != -1)
            {
                break;
            }

            fnName = s + fnName;
            count++;
        }

        return fnName;
    }


    private string GetRightName(string expr)
    {
        int len = expr.Length;
        string fnName = "";
        for (int i = 0; i < len; i++)
        {
            string s = expr.Substring(i, 1);
            if (s == "<" || s == ">" || s == "|" || s == "&" || s == "=")
            {
                if (this.Interrupt.IndexOf(s + expr.Substring(i + 1, 1)) != -1)
                {
                    s += expr.Substring(i + 1, 1);
                    i = i + 1;
                }
            }
            if (this.Interrupt.IndexOf(s) != -1)
            {
                break;
            }

            fnName = fnName + s;
        }

        return fnName;
    }

    private object GetValue(string val)
    {
        if (val == "false" || val == "False")
        {
            return false;
        }
        if (val == "true" || val == "True")
        {
            return true;
        }
        if (this.ValueDic.ContainsKey(val))
        {
            return (this.ValueDic[val]);
        }
        else
        {
            double temp;
            if(double.TryParse(val,out temp)) //(Regex.IsMatch(val, @"[0-9]+[.]?$"))
            {
                //是数字
                return temp;
            }

            return val;
        }
    }

    /// <summary>
    /// 处理表达式运算
    /// </summary>
    /// <param name="expression"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    private string Deal(string expression, string token)
    {
        int tokenIndex = expression.IndexOf(token);
        if (tokenIndex == -1)
            return expression;

        if (token == "!")
        {
            int nonSignIndex = expression.IndexOf("!");
            string nextSign = expression.Substring(nonSignIndex + 1, 1);
            if (nextSign == "=")
            {
                token = "!=";
            }
        }
        string a = "";
        string b = "";
        string ls;
        string rs;
        object lv = null;
        object rv = null;
        object tv = null;
        double lvdouble = 0;
        double rvdouble = 0;

        if (token == "!")
        {
            int nonSignIndex = expression.IndexOf("!");
            while (nonSignIndex >= 0)
            {
                a = expression.Substring(0, nonSignIndex);
                int bSignStart = nonSignIndex + 1;
                b = expression.Substring(bSignStart, expression.Length - bSignStart);
                ls = this.GetLeftName(a);
                rs = this.GetRightName(b);
                //lv = this.GetValue(ls);
                rv = this.GetValue(rs);
                tv = !Convert.ToBoolean(rv);

                a = a.Substring(0, a.Length - ls.Length);
                b = b.Substring(rs.Length, b.Length - rs.Length);
                expression = a + tv + b;

                nonSignIndex = expression.IndexOf("!");
            }
        }
        else
        {
            int tokenSize = token.Length;
            a = expression.Substring(0, tokenIndex);
            b = expression.Substring(tokenIndex + tokenSize, expression.Length - tokenIndex - tokenSize);
            ls = this.GetLeftName(a);
            rs = this.GetRightName(b);
            lv = this.GetValue(ls);
            rv = this.GetValue(rs);
            bool lvIsDouble = double.TryParse(lv.ToString(),out lvdouble);
            bool rvIsDouble = double.TryParse(rv.ToString(), out rvdouble); 
            switch (token)
            {
                case "+":
                    tv = lvdouble + rvdouble;
                    break;
                case "-":
                    tv = lvdouble - rvdouble;
                    break;
                case "*":
                    tv = lvdouble * rvdouble;
                    break;
                case "/":
                    tv = lvdouble / rvdouble;
                    break;
                case ">":
                    tv = lvdouble > rvdouble;
                    break;
                case ">=":
                    tv = lvdouble >= rvdouble;
                    break;
                case "<":
                    tv = lvdouble < rvdouble;
                    break;
                case "<=":
                    tv = lvdouble <= rvdouble;
                    break;
                case "==":
                    if (lvIsDouble && rvIsDouble)
                    {
                        //是数字
                        tv = lvdouble == rvdouble;
                    }
                    else
                    {
                        //布尔值
                        tv = Convert.ToBoolean(lv) == Convert.ToBoolean(rv);
                    }
                    break;
                case "&&":
                    tv = Convert.ToBoolean(lv) && Convert.ToBoolean(rv);
                    break;
                case "||":
                    tv = Convert.ToBoolean(lv) || Convert.ToBoolean(rv);
                    break;
                case "!=":
                    if (lvIsDouble && rvIsDouble)
                    {
                        //是数字
                        tv = lvdouble != rvdouble;
                    }
                    else
                    {
                        //布尔值
                        tv = Convert.ToBoolean(lv) != Convert.ToBoolean(rv);
                    }
                    break;

            }

            a = a.Substring(0, a.Length - ls.Length);
            b = b.Substring(rs.Length, b.Length - rs.Length);

            expression = a + tv + b;
        }

        return expression;
    }
}

#endif