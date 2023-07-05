using System;
using System.Collections.Generic;
using UnityEngine;

public class CalculateArenaUtils
{
    /**奖励计算公式*/
    private static ExpressionParser formula;
    /**全局属性公式集合，为null则不需要重新计算覆盖*/
    private static Dictionary<string, string> alterValueFormula;
    /**战斗力公式参数 */
    public static List<string> alterTypeList;
    /**潜力集合*/
    private static Dictionary<string, float> alterGrow;

    public static int CalculateArenaRewardProp(Dictionary<string, int> vlue, string script = "")
    {
        if (alterValueFormula == null)
        {
            alterValueFormula = new Dictionary<string, string>();
        }

        return calculatePropFormula(vlue, script);
    }

    private static Dictionary<string, float> mergeProp(Dictionary<string, float> models, Dictionary<string, float> result)
    {
        float resutlValue;
        foreach (var item in models)
        {
            if (result.TryGetValue(item.Key, out resutlValue))
            {
                result[item.Key] = resutlValue + item.Value;
            }
            else
            {
                result.Add(item.Key, item.Value);
            }
        }
        return result;
    }

    public static int calculatePropFormula(Dictionary<string, int> result, string script = "")
    {
        if (formula == null)
        {
            formula = new ExpressionParser();
        }
        formula.AddFunction("floor", floor);
        formula.AddFunction("int", Int);
        formula.AddFunction("getValue", getValue);
        formula.AddFunction("pow", pow);
        formula.AddFunction("min", min);
        formula.AddFunction("max", max);

        foreach (KeyValuePair<string, int> item in result)
        {
            formula.AddVariable(item.Key, item.Value);
        }

        object fightScoreObj = formula.run(script);
        if (fightScoreObj != null)
        {
            return int.Parse(fightScoreObj.ToString());
        }
        else
        {
            return 0;
        }
    }

    private static Dictionary<string, float> m_ParamObj;

    private static object floor(object[] param)
    {
        float numValue = 0;
        float.TryParse(param[0].ToString(), out numValue);
        return Math.Floor(numValue);
    }

    private static object Int(object[] param)
    {
        int numValue = 0;
        int.TryParse(param[0].ToString(), out numValue);
        return numValue;
    }

    private static object pow(object[] param)
    {
        float a = 0;
        float b = 0;
        float.TryParse(param[0].ToString(), out a);
        float.TryParse(param[1].ToString(), out b);
        return Math.Pow(a, b);
    }

    private static object min(object[] param)
    {
        float a = 0;
        float b = 0;
        float.TryParse(param[0].ToString(), out a);
        float.TryParse(param[1].ToString(), out b);
        return Math.Min(a, b);
    }

    private static object max(object[] param)
    {
        float a = 0;
        float b = 0;
        float.TryParse(param[0].ToString(), out a);
        float.TryParse(param[1].ToString(), out b);
        return Math.Max(a, b);
    }

    private static object getValue(object[] param)
    {
        string paramKey = param[0].ToString();
        if (m_ParamObj.ContainsKey(paramKey))
        {
            return m_ParamObj[paramKey];
        }
        return 0;
    }
}

public class ArenaRewardPropData
{
    public Dictionary<string, float> valueMap;
    public Dictionary<string, float> ratesMap;
    public float score;
}