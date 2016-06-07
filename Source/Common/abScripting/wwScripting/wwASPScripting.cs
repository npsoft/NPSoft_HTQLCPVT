
using System;
using System.Text;

using Westwind.Tools;



namespace Westwind.wwScripting
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class wwASPScripting
	{
		public wwScripting oScript = null;
		public string cErrormsg = "";
		public bool bError = false;

		public wwASPScripting()
		{
			//
			// TODO: Add constructor logic here
			//
			this.oScript = new wwScripting("CSharp");
		}


		/// <summary>
		/// Parses a script into a compilable program
		/// </summary>
		/// <param name="lcCode"></param>
		/// <returns></returns>
		public string ParseScript(string lcCode)
		{
			if (lcCode == null)
				return "";
			
			wwScriptResponse oSb = new wwScriptResponse();

			int lnLast = 0;
			int lnAt2 = 0;
			int lnAt = lcCode.IndexOf("<%",0);
			if (lnAt == -1)
				return lcCode;

			// *** Create the Response object which is used to write output into the code generator
			oSb.Append("wwScriptResponse Response = new wwScriptResponse();\r\n");

			while (lnAt > -1) {
				if (lnAt > -1)    			 	
				    // *** Catch the plain text write out to the Response Stream as is - fix up for quotes
					oSb.Append("Response.oSb.Append(@\"" + lcCode.Substring(lnLast,lnAt - lnLast).Replace("\"","\"\"") + "\" );\r\n\r\n");
				
				//*** Find end tag
				lnAt2 = lcCode.IndexOf("%>",lnAt);
				if (lnAt2 < 0)
					break;

				string lcSnippet = lcCode.Substring(lnAt,lnAt2-lnAt + 2);
				if (lcSnippet.Substring(2,1) == "=")
					// *** Write out an expression. 'Eval' inside of a Response.Write call
					oSb.Append("Response.oSb.Append(" + lcSnippet.Substring(3,lcSnippet.Length-5).Trim() + ".ToString());\r\n");
				else if (lcSnippet.Substring(2,1) == "@") 
				{
					string lcAttribute = "";

					// *** Handle Directives
					lcAttribute = wwUtils.StrExtract(lcSnippet,"Assembly","=");
					if (lcAttribute.Length > 0) 
					{
						lcAttribute = wwUtils.StrExtract(lcSnippet,"\"","\"");
						if (lcAttribute.Length > 0)
							this.oScript.AddAssembly(lcAttribute);
					}		
					else {
						lcAttribute = wwUtils.StrExtract(lcSnippet,"Import","=");
						if (lcAttribute.Length > 0) 
						{
							lcAttribute = wwUtils.StrExtract(lcSnippet,"\"","\"");
							if (lcAttribute.Length > 0)
								this.oScript.AddNamespace(lcAttribute);
						}		
					}
				}	
				else
					// *** Write out a line of code as is.
					oSb.Append(lcSnippet.Substring(2,lcSnippet.Length - 4) + "\r\n");

				lnLast = lnAt2 + 2;
				lnAt = lcCode.IndexOf("<%",lnLast);
				if (lnAt < 0)
				   // *** Write out the final block of non-code text
					oSb.Append("Response.oSb.Append(@\"" + lcCode.Substring(lnLast,lcCode.Length - lnLast).Replace("\"","\"\"") + "\" );\r\n\r\n");
			}

			oSb.Append("return Response.oSb.ToString();");

			return oSb.ToString();
		}
		public void Release()
		{
			this.oScript.Release();
		}

	}

	public class wwScriptResponse
	{
		public StringBuilder oSb;

		public wwScriptResponse() 
		{
			this.oSb = new StringBuilder();
		}
		public void Write(string lcString) 
		{
			this.oSb.Append(lcString);
		}
		public void Append(string lcString)
		{
			this.oSb.Append(lcString);
		}
		public override string ToString() 
		{
			return this.oSb.ToString();
		}
	}

}
