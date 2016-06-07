using System;
using System.IO;
using System.Text;
//using System.Collections.Specialized;
//using System.Collections;

using Microsoft.CSharp;
using Microsoft.VisualBasic;
using System.Reflection;
using System.Runtime.Remoting;
using System.CodeDom.Compiler;


using Westwind.RemoteLoader;

namespace Westwind.wwScripting
{
	/// <summary>
	/// Deletgate for the Completed Event
	/// </summary>
	public delegate void DelegateCompleted(object sender,EventArgs e);

	/// <summary>
	/// Class that enables running of code dynamcially created at runtime.
	/// Provides functionality for evaluating and executing compiled code.
	/// </summary>
    public class wwScripting
	{
		/// <summary>
		/// Compiler object used to compile our code
		/// </summary>
		protected ICodeCompiler oCompiler = null;

		/// <summary>
		/// Reference to the Compiler Parameter object
		/// </summary>
		protected CompilerParameters oParameters = null;

		/// <summary>
		/// Reference to the final assembly
		/// </summary>
		protected Assembly oAssembly = null;

		/// <summary>
		/// The compiler results object used to figure out errors.
		/// </summary>
		protected CompilerResults oCompiled = null;
		protected string cOutputAssembly = null;
		protected string cNamespaces = "";
		protected bool lFirstLoad = true;



		/// <summary>
		/// The object reference to the compiled object available after the first method call.
		/// You can use this method to call additional methods on the object.
		/// For example, you can use CallMethod and pass multiple methods of code each of
		/// which can be executed indirectly by using CallMethod() on this object reference.
		/// </summary>
		public object oObjRef = null;

		/// <summary>
		/// If true saves source code before compiling to the cSourceCode property.
		/// </summary>
		public bool lSaveSourceCode = false;

		/// <summary>
		/// Contains the source code of the entired compiled assembly code.
		/// Note: this is not the code passed in, but the full fixed assembly code.
		/// Only set if lSaveSourceCode=true.
		/// </summary>
		public string cSourceCode = "";

		/// <summary>
		/// Line where the code that runs starts
		/// </summary>
		protected int nStartCodeLine = 0;

		/// <summary>
		/// Namespace of the assembly created by the script processor. Determines
		/// how the class will be referenced and loaded.
		/// </summary>
		public string cAssemblyNamespace = "WestWindScripting";

		/// <summary>
		/// Name of the class created by the script processor. Script code becomes methods in the class.
		/// </summary>
		public string cClassName = "WestWindScript";

		/// <summary>
		/// Determines if default assemblies are added. System, System.IO, System.Reflection
		/// </summary>
		public bool lDefaultAssemblies = true;

		protected AppDomain oAppDomain = null;

		public string cErrorMsg = "";
		public bool bError = false;

		/// <summary>
		/// Path for the support assemblies wwScripting and RemoteLoader.
		/// By default this can be blank but if you're using this functionality
		/// under ASP.Net specify the bin path explicitly. Should include trailing
		/// dash.
		/// </summary>
		//[Description("Path for the support assemblies wwScripting and RemoteLoader. Blank by default. Include trailing dash.")]
		public string cSupportAssemblyPath = "";

		/// <summary>
		/// The scripting language used. CSharp, VB, JScript
		/// </summary>
		public string cScriptingLanguage = "CSharp";
		
		/// <summary>
		/// The language to be used by this scripting class. Currently only C# is supported 
		/// with VB syntax available but not tested.
		/// </summary>
		/// <param name="lcLanguage">CSharp or VB</param>
		public wwScripting(string lcLanguage)
		{			
			this.SetLanguage(lcLanguage);
		}
		public wwScripting() 
		{
			this.SetLanguage("CSharp");
		}

		

		/// <summary>
		/// Specifies the language that is used. Supported languages include
		/// CSHARP C# VB
		/// </summary>
		/// <param name="lcLanguage"></param>
		public void SetLanguage(string lcLanguage) 
		{
			this.cScriptingLanguage = lcLanguage;

			if (this.cScriptingLanguage == "CSharp" || this.cScriptingLanguage == "C#") 
			{
				this.oCompiler = new CSharpCodeProvider().CreateCompiler();
				this.cScriptingLanguage = "CSharp";
			}	
			else if (this.cScriptingLanguage == "VB")	
			{
				this.oCompiler = new VBCodeProvider().CreateCompiler();
			}										   
			// else throw(Exception ex);

			this.oParameters = new CompilerParameters();
		}


		/// <summary>
		/// Adds an assembly to the compiled code
		/// </summary>
		/// <param name="lcAssemblyDll">DLL assembly file name</param>
		/// <param name="lcNamespace">Namespace to add if any. Pass null if no namespace is to be added</param>
		public void AddAssembly(string lcAssemblyDll,string lcNamespace) 
		{
			if (lcAssemblyDll==null && lcNamespace == null) 
			{
				// *** clear out assemblies and namespaces
				this.oParameters.ReferencedAssemblies.Clear();
				this.cNamespaces = "";
				return;
			}
			
			if (lcAssemblyDll != null)
				this.oParameters.ReferencedAssemblies.Add(lcAssemblyDll);
		
			if (lcNamespace != null) 
				if (this.cScriptingLanguage == "CSharp")
					this.cNamespaces = this.cNamespaces + "using " + lcNamespace + ";\r\n";
				else
					this.cNamespaces = this.cNamespaces + "imports " + lcNamespace + "\r\n";
		}

		/// <summary>
		/// Adds an assembly to the compiled code.
		/// </summary>
		/// <param name="lcAssemblyDll">DLL assembly file name</param>
		public void AddAssembly(string lcAssemblyDll) 
		{
			this.AddAssembly(lcAssemblyDll,null);
		}
		public void AddNamespace(string lcNamespace)
		{
			this.AddAssembly(null,lcNamespace);
		}
		public void AddDefaultAssemblies()
		{
			this.AddAssembly("System.dll","System");
			this.AddNamespace("System.Reflection");
			this.AddNamespace("System.IO");
		}


		/// <summary>
		/// Executes a complete method by wrapping it into a class.
		/// </summary>
		/// <param name="lcCode">One or more complete methods.</param>
		/// <param name="lcMethodName">Name of the method to call.</param>
		/// <param name="loParameters">any number of variable parameters</param>
		/// <returns></returns>
		public object ExecuteMethod(string lcCode, string lcMethodName, params object[] loParameters) 
		{
			
			if (this.oObjRef == null) 
			{
				if (this.lFirstLoad)
				{
					if (this.lDefaultAssemblies) 
					{
						this.AddDefaultAssemblies();
					}
					this.AddAssembly(this.cSupportAssemblyPath + "RemoteLoader.dll","Westwind.RemoteLoader");
					this.AddAssembly(this.cSupportAssemblyPath + "wwScripting.dll","Westwind.wwScripting");
					this.lFirstLoad = false;
				}

				StringBuilder sb = new StringBuilder("");

				//*** Program lead in and class header
				sb.Append(this.cNamespaces);
				sb.Append("\r\n");

				if (this.cScriptingLanguage == "CSharp") 
				{
					// *** Namespace headers and class definition
					sb.Append("namespace " + this.cAssemblyNamespace + "{\r\npublic class " + this.cClassName + ":MarshalByRefObject,IRemoteInterface {\r\n");	
				
					// *** Generic Invoke method required for the remote call interface
					sb.Append(
						"public object Invoke(string lcMethod,object[] parms) {\r\n" + 
						"return this.GetType().InvokeMember(lcMethod,BindingFlags.InvokeMethod,null,this,parms );\r\n" +
						"}\r\n\r\n" );

					//*** The actual code to run in the form of a full method definition.
					sb.Append(lcCode);

					sb.Append("\r\n} }");  // Class and namespace closed
				}
				else if (this.cScriptingLanguage == "VB") 
				{
					// *** Namespace headers and class definition
					sb.Append("Namespace " + this.cAssemblyNamespace + "\r\npublic class " + this.cClassName + "\r\n");
					sb.Append("Inherits MarshalByRefObject\r\nImplements IRemoteInterface\r\n\r\n");	
				
					// *** Generic Invoke method required for the remote call interface
					sb.Append(
						"Public Overridable Overloads Function Invoke(ByVal lcMethod As String, ByVal Parameters() As Object) As Object _\r\n" +
						"Implements IRemoteInterface.Invoke\r\n" + 
						"return me.GetType().InvokeMember(lcMethod,BindingFlags.InvokeMethod,nothing,me,Parameters)\r\n" +
						"End Function\r\n\r\n" );

					//*** The actual code to run in the form of a full method definition.
					sb.Append(lcCode);

					sb.Append("\r\n\r\nEnd Class\r\nEnd Namespace\r\n");  // Class and namespace closed
				}

				if (this.lSaveSourceCode)
				{
					this.cSourceCode = sb.ToString();
					//MessageBox.Show(this.cSourceCode);
				}

				if (!this.CompileAssembly(sb.ToString()) )
					return null;

				object loTemp = this.CreateInstance();
				if (loTemp == null)
					return null;
			}

			return this.CallMethod(this.oObjRef,lcMethodName,loParameters);
		}

		/// <summary>
		///  Executes a snippet of code. Pass in a variable number of parameters
		///  (accessible via the loParameters[0..n] array) and return an object parameter.
		///  Code should include:  return (object) SomeValue as the last line or return null
		/// </summary>
		/// <param name="lcCode">The code to execute</param>
		/// <param name="loParameters">The parameters to pass the code</param>
		/// <returns></returns>
		public object ExecuteCode(string lcCode, params object[] loParameters) 
		{	
			if (this.cScriptingLanguage == "CSharp")
				return this.ExecuteMethod("public object ExecuteCode(params object[] Parameters) {" + 
						lcCode + 
						"}",
						"ExecuteCode",loParameters);
			else if (this.cScriptingLanguage == "VB")
				return this.ExecuteMethod("public function ExecuteCode(ParamArray Parameters() As Object) as object\r\n" + 
					lcCode + 
					"\r\nend function\r\n",
					"ExecuteCode",loParameters);

			return null;
		}
	    
		/// <summary>
		/// Compiles and runs the source code for a complete assembly.
		/// </summary>
		/// <param name="lcSource"></param>
		/// <returns></returns>
		public bool CompileAssembly(string lcSource) 
		{
			//this.oParameters.GenerateExecutable = false;

			if (this.oAppDomain == null && this.cOutputAssembly == null)
				this.oParameters.GenerateInMemory = true;
			else if (this.oAppDomain != null && this.cOutputAssembly == null)
			{
				// *** Generate an assembly of the same name as the domain
				this.cOutputAssembly = "wws_" + Guid.NewGuid().ToString() + ".dll";
				this.oParameters.OutputAssembly = this.cOutputAssembly;
			}
			else {
				  this.oParameters.OutputAssembly = this.cOutputAssembly;
			}
		
			this.oCompiled = this.oCompiler.CompileAssemblyFromSource(this.oParameters,lcSource);

			if (oCompiled.Errors.HasErrors) 
			{
				this.bError = true;

				// *** Create Error String
				this.cErrorMsg = oCompiled.Errors.Count.ToString() + " Errors:";
				for (int x=0;x<oCompiled.Errors.Count;x++) 
					this.cErrorMsg = this.cErrorMsg  + "\r\nLine: " + oCompiled.Errors[x].Line.ToString() + " - " + 
						                               oCompiled.Errors[x].ErrorText;				
				return false;
			}

			if (this.oAppDomain == null)
				this.oAssembly = oCompiled.CompiledAssembly;
			
			return true;
		}

		public object CreateInstance() 
		{
			if (this.oObjRef != null) 
			{
				return this.oObjRef;
			}
			
			// *** Create an instance of the new object
			try 
			{
				if (this.oAppDomain == null)
					try 
					{
						this.oObjRef =  oAssembly.CreateInstance(this.cAssemblyNamespace + "." + this.cClassName);
						return this.oObjRef;
					}
					catch(Exception ex) 
					{
						this.bError = true;
						this.cErrorMsg = ex.Message;
						return null;
					}
				else 
				{
					// create the factory class in the secondary app-domain
					RemoteLoaderFactory factory = (RemoteLoaderFactory) this.oAppDomain.CreateInstance( "RemoteLoader", "Westwind.RemoteLoader.RemoteLoaderFactory" ).Unwrap();

					// with the help of this factory, we can now create a real 'LiveClass' instance
					this.oObjRef = factory.Create( this.cOutputAssembly, this.cAssemblyNamespace + "." + this.cClassName, null );

					return this.oObjRef;			
				}	
			}
			catch(Exception ex) 
			{
				this.bError = true;
				this.cErrorMsg = ex.Message;
				return null;
			}
				
		}

		public object CallMethod(object loObject,string lcMethod, params object[] loParameters) 
		{
			// *** Try to run it
			try 
			{
				if (this.oAppDomain == null)
					// *** Just invoke the method directly through Reflection
					return loObject.GetType().InvokeMember(lcMethod,BindingFlags.InvokeMethod,null,loObject,loParameters );
				else 
				{
					// *** Invoke the method through the Remote interface and the Invoke method
					object loResult;
					try 
					{
						// *** Cast the object to the remote interface to avoid loading type info
						IRemoteInterface loRemote = (IRemoteInterface) loObject;

						// *** Indirectly call the remote interface
						loResult = loRemote.Invoke(lcMethod,loParameters);
					}
					catch(Exception ex) 
					{
						this.bError = true;
						this.cErrorMsg = ex.Message;
						return null;
					}
					return loResult;
				}	
			}
			catch(Exception ex) 
			{
				this.bError = true;
				this.cErrorMsg = ex.Message;
			}
			return null;
		}

		public bool CreateAppDomain(string lcAppDomain) 
		{
			if (lcAppDomain == null)
				lcAppDomain = "wwscript";

			AppDomainSetup loSetup = new AppDomainSetup();
			loSetup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;

			this.oAppDomain = AppDomain.CreateDomain(lcAppDomain,null,loSetup);
			return true;
		}

		public bool UnloadAppDomain()
		{
			if (this.oAppDomain != null)
			   AppDomain.Unload(this.oAppDomain);

			this.oAppDomain = null;

			if (this.cOutputAssembly != null) 
			{
				try 
				{
					File.Delete(this.cOutputAssembly);
				}
				catch(Exception) {;}
			}

			return true;
		}
		public void Release() 
		{
			this.oObjRef = null;
		}

		public void Dispose() 
		{
			this.Release();
			this.UnloadAppDomain();
		}

		~wwScripting() 
		{
			this.Dispose();
		}
	}


}