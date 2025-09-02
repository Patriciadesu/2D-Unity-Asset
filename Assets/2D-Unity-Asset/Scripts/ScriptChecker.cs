using System;
using UnityEngine;

public static class ScriptChecker 
{
    public static bool CheckIfScriptExist(string scriptName)
    {
        Type type = Type.GetType(scriptName);

        if (type != null)
            return true;
        else
            return false;
    }
}
