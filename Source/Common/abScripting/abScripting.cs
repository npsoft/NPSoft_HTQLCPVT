using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PhotoBookmart.Web.abScripting
{
    public enum abScriptingLanguageEnum
    {
        CSharp,
        VisualBasic
    }

    public enum abScriptingFunctionEnum
    {
        DefaultValue,
        Validation
    }

    public class abScriptingRuntimeModel
    {
        public string Error { get; set; }
        public string Result { get; set; }
        public bool Status { get; set; }
    }

    public class abScripting
    {
        public static abScriptingRuntimeModel DLR(string code, string bin_path, abScriptingFunctionEnum function_type= abScriptingFunctionEnum.DefaultValue,  abScriptingLanguageEnum language = abScriptingLanguageEnum.CSharp, params object[] p)
        {
            string lang_text = "C#";
            if (language == abScriptingLanguageEnum.VisualBasic)
            {
                lang_text = "VB";
            }
            wwScripting loScript = new wwScripting(lang_text);
            loScript.lSaveSourceCode = true;

            if (!bin_path.EndsWith("\\"))
                bin_path += "\\";
            loScript.cSupportAssemblyPath = bin_path;

            //loScript.CreateAppDomain("WestWind");  // force into AppDomain

            loScript.AddAssembly("system.windows.forms.dll", "System.Windows.Forms");
            loScript.AddAssembly("system.web.dll", "System.Web");
            loScript.AddNamespace("System.Net");

            if (p == null)
            {
                p = new object[] { };
            }

            abScriptingRuntimeModel m = new abScriptingRuntimeModel();
            m.Error = "";
            m.Result = "";
            m.Status = true;

            try
            {
                if (function_type == abScriptingFunctionEnum.DefaultValue)
                {
                    m.Result = loScript.NetDefaultValue(code).ToString();
                }
                else
                {
                    m.Result = loScript.NetValidation(code,p).ToString();
                }
                m.Status = true;

                //*** Execute full method or mutliple methods on the same object
                //			string lcResult = (string) loScript.ExecuteMethod(lcCode,"Test","rick strahl",(int) x);
                //			lcResult =  (string) loScript.CallMethod(loScript.oObjRef,"Test2","rick strahl",(int) x);
            }
            catch
            {
                m.Error = loScript.cErrorMsg;
                m.Status = false;
            }
            finally
            {
                loScript.Dispose();
            }

            return m;
        }
    }
}
