//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SolutionGen.Templates {
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using SolutionGen.Compiling.Model;
    using System;
    
    
    public partial class DotNetProject : DotNetProjectBase {
        
        public virtual string TransformText() {
            this.GenerationEnvironment = null;
            
            #line 7 ".\Templates\DotNetProject.tt"
            this.Write(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""12.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
    <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props""
            Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')""/>
    <PropertyGroup>
        <Configuration Condition="" '$(Configuration)' == '' "">");
            
            #line default
            #line hidden
            
            #line 12 ".\Templates\DotNetProject.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture( DefaultConfiguration ));
            
            #line default
            #line hidden
            
            #line 12 ".\Templates\DotNetProject.tt"
            this.Write("</Configuration>\r\n        <Platform Condition=\" \'$(Platform)\' == \'\' \">");
            
            #line default
            #line hidden
            
            #line 13 ".\Templates\DotNetProject.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture( DefaultPlatform ));
            
            #line default
            #line hidden
            
            #line 13 ".\Templates\DotNetProject.tt"
            this.Write("</Platform>\r\n        <ProjectGuid>{");
            
            #line default
            #line hidden
            
            #line 14 ".\Templates\DotNetProject.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture( ProjectGuid ));
            
            #line default
            #line hidden
            
            #line 14 ".\Templates\DotNetProject.tt"
            this.Write("}</ProjectGuid>\r\n        <OutputType>Library</OutputType>\r\n        <AppDesignerFo" +
                    "lder>Properties</AppDesignerFolder>\r\n        <RootNamespace>");
            
            #line default
            #line hidden
            
            #line 17 ".\Templates\DotNetProject.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture( RootNamespace ));
            
            #line default
            #line hidden
            
            #line 17 ".\Templates\DotNetProject.tt"
            this.Write("</RootNamespace>\r\n        <AssemblyName>");
            
            #line default
            #line hidden
            
            #line 18 ".\Templates\DotNetProject.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture( Project.Name ));
            
            #line default
            #line hidden
            
            #line 18 ".\Templates\DotNetProject.tt"
            this.Write("</AssemblyName>\r\n        <TargetFrameworkVersion>");
            
            #line default
            #line hidden
            
            #line 19 ".\Templates\DotNetProject.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture( TargetFrameworkVersion ));
            
            #line default
            #line hidden
            
            #line 19 ".\Templates\DotNetProject.tt"
            this.Write("</TargetFrameworkVersion>\r\n        <LangVersion>");
            
            #line default
            #line hidden
            
            #line 20 ".\Templates\DotNetProject.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture( LanguageVersion ));
            
            #line default
            #line hidden
            
            #line 20 ".\Templates\DotNetProject.tt"
            this.Write("</LangVersion>\r\n        <FileAlignment>512</FileAlignment>\r\n    </PropertyGroup>\r" +
                    "\n");
            
            #line default
            #line hidden
            
            #line 23 ".\Templates\DotNetProject.tt"
 foreach (string configuration in Solution.ActiveConfigurations.Keys)
{
    foreach (string platform in TargetPlatforms)
    {
            
            #line default
            #line hidden
            
            #line 27 ".\Templates\DotNetProject.tt"
            this.Write("    <PropertyGroup Condition=\" \'$(Configuration)|$(Platform)\' == \'");
            
            #line default
            #line hidden
            
            #line 27 ".\Templates\DotNetProject.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture( configuration ));
            
            #line default
            #line hidden
            
            #line 27 ".\Templates\DotNetProject.tt"
            this.Write("|");
            
            #line default
            #line hidden
            
            #line 27 ".\Templates\DotNetProject.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture( platform ));
            
            #line default
            #line hidden
            
            #line 27 ".\Templates\DotNetProject.tt"
            this.Write("\' \">\r\n        <PlatformTarget>");
            
            #line default
            #line hidden
            
            #line 28 ".\Templates\DotNetProject.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture( GetStringProperty(configuration, Settings.PROP_CONFIGURATION_PLATFORM_TARGET) ));
            
            #line default
            #line hidden
            
            #line 28 ".\Templates\DotNetProject.tt"
            this.Write("</PlatformTarget>\r\n        <DebugSymbols>");
            
            #line default
            #line hidden
            
            #line 29 ".\Templates\DotNetProject.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture( GetStringProperty(configuration, Settings.PROP_DEBUG_SYMBOLS) ));
            
            #line default
            #line hidden
            
            #line 29 ".\Templates\DotNetProject.tt"
            this.Write("</DebugSymbols>\r\n        <DebugType>");
            
            #line default
            #line hidden
            
            #line 30 ".\Templates\DotNetProject.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture( GetStringProperty(configuration, Settings.PROP_DEBUG_TYPE) ));
            
            #line default
            #line hidden
            
            #line 30 ".\Templates\DotNetProject.tt"
            this.Write("</DebugType>\r\n        <Optimize>");
            
            #line default
            #line hidden
            
            #line 31 ".\Templates\DotNetProject.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture( GetStringProperty(configuration, Settings.PROP_OPTIMIZE) ));
            
            #line default
            #line hidden
            
            #line 31 ".\Templates\DotNetProject.tt"
            this.Write("</Optimize>\r\n        <OutputPath>bin\\");
            
            #line default
            #line hidden
            
            #line 32 ".\Templates\DotNetProject.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture( configuration ));
            
            #line default
            #line hidden
            
            #line 32 ".\Templates\DotNetProject.tt"
            this.Write("\\</OutputPath>\r\n        <DefineConstants>");
            
            #line default
            #line hidden
            
            #line 33 ".\Templates\DotNetProject.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture( GetDefineConstants(configuration) ));
            
            #line default
            #line hidden
            
            #line 33 ".\Templates\DotNetProject.tt"
            this.Write("</DefineConstants>\r\n        <ErrorReport>");
            
            #line default
            #line hidden
            
            #line 34 ".\Templates\DotNetProject.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture( GetStringProperty(configuration, Settings.PROP_ERROR_REPORT) ));
            
            #line default
            #line hidden
            
            #line 34 ".\Templates\DotNetProject.tt"
            this.Write("</ErrorReport>\r\n        <WarningLevel>");
            
            #line default
            #line hidden
            
            #line 35 ".\Templates\DotNetProject.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture( GetStringProperty(configuration, Settings.PROP_WARNING_LEVEL) ));
            
            #line default
            #line hidden
            
            #line 35 ".\Templates\DotNetProject.tt"
            this.Write("</WarningLevel>\r\n    </PropertyGroup>\r\n");
            
            #line default
            #line hidden
            
            #line 37 ".\Templates\DotNetProject.tt"
  }
}
            
            #line default
            #line hidden
            
            #line 39 ".\Templates\DotNetProject.tt"
            this.Write(@"    <ItemGroup>
        <Reference Include=""System""/>
        <Reference Include=""System.Core""/>
        <Reference Include=""System.Data""/>
        <Reference Include=""System.Xml""/>
    </ItemGroup>
    <ItemGroup>
        <Compile Include=""Class1.cs""/>
        <Compile Include=""Properties\AssemblyInfo.cs""/>
    </ItemGroup>
    <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets""/>
    <!-- To modify your build process, add your task inside one of the targets below and uncomment it. 
         Other similar extension points exist, see Microsoft.Common.targets.
    <Target Name=""BeforeBuild"">
    </Target>
    <Target Name=""AfterBuild"">
    </Target>
    -->

</Project>
");
            
            #line default
            #line hidden
            return this.GenerationEnvironment.ToString();
        }
        
        public virtual void Initialize() {
        }
    }
    
    public class DotNetProjectBase {
        
        private global::System.Text.StringBuilder builder;
        
        private global::System.Collections.Generic.IDictionary<string, object> session;
        
        private global::System.CodeDom.Compiler.CompilerErrorCollection errors;
        
        private string currentIndent = string.Empty;
        
        private global::System.Collections.Generic.Stack<int> indents;
        
        private ToStringInstanceHelper _toStringHelper = new ToStringInstanceHelper();
        
        public virtual global::System.Collections.Generic.IDictionary<string, object> Session {
            get {
                return this.session;
            }
            set {
                this.session = value;
            }
        }
        
        public global::System.Text.StringBuilder GenerationEnvironment {
            get {
                if ((this.builder == null)) {
                    this.builder = new global::System.Text.StringBuilder();
                }
                return this.builder;
            }
            set {
                this.builder = value;
            }
        }
        
        protected global::System.CodeDom.Compiler.CompilerErrorCollection Errors {
            get {
                if ((this.errors == null)) {
                    this.errors = new global::System.CodeDom.Compiler.CompilerErrorCollection();
                }
                return this.errors;
            }
        }
        
        public string CurrentIndent {
            get {
                return this.currentIndent;
            }
        }
        
        private global::System.Collections.Generic.Stack<int> Indents {
            get {
                if ((this.indents == null)) {
                    this.indents = new global::System.Collections.Generic.Stack<int>();
                }
                return this.indents;
            }
        }
        
        public ToStringInstanceHelper ToStringHelper {
            get {
                return this._toStringHelper;
            }
        }
        
        public void Error(string message) {
            this.Errors.Add(new global::System.CodeDom.Compiler.CompilerError(null, -1, -1, null, message));
        }
        
        public void Warning(string message) {
            global::System.CodeDom.Compiler.CompilerError val = new global::System.CodeDom.Compiler.CompilerError(null, -1, -1, null, message);
            val.IsWarning = true;
            this.Errors.Add(val);
        }
        
        public string PopIndent() {
            if ((this.Indents.Count == 0)) {
                return string.Empty;
            }
            int lastPos = (this.currentIndent.Length - this.Indents.Pop());
            string last = this.currentIndent.Substring(lastPos);
            this.currentIndent = this.currentIndent.Substring(0, lastPos);
            return last;
        }
        
        public void PushIndent(string indent) {
            this.Indents.Push(indent.Length);
            this.currentIndent = (this.currentIndent + indent);
        }
        
        public void ClearIndent() {
            this.currentIndent = string.Empty;
            this.Indents.Clear();
        }
        
        public void Write(string textToAppend) {
            this.GenerationEnvironment.Append(textToAppend);
        }
        
        public void Write(string format, params object[] args) {
            this.GenerationEnvironment.AppendFormat(format, args);
        }
        
        public void WriteLine(string textToAppend) {
            this.GenerationEnvironment.Append(this.currentIndent);
            this.GenerationEnvironment.AppendLine(textToAppend);
        }
        
        public void WriteLine(string format, params object[] args) {
            this.GenerationEnvironment.Append(this.currentIndent);
            this.GenerationEnvironment.AppendFormat(format, args);
            this.GenerationEnvironment.AppendLine();
        }
        
        public class ToStringInstanceHelper {
            
            private global::System.IFormatProvider formatProvider = global::System.Globalization.CultureInfo.InvariantCulture;
            
            public global::System.IFormatProvider FormatProvider {
                get {
                    return this.formatProvider;
                }
                set {
                    if ((value != null)) {
                        this.formatProvider = value;
                    }
                }
            }
            
            public string ToStringWithCulture(object objectToConvert) {
                if ((objectToConvert == null)) {
                    throw new global::System.ArgumentNullException("objectToConvert");
                }
                global::System.Type type = objectToConvert.GetType();
                global::System.Type iConvertibleType = typeof(global::System.IConvertible);
                if (iConvertibleType.IsAssignableFrom(type)) {
                    return ((global::System.IConvertible)(objectToConvert)).ToString(this.formatProvider);
                }
                global::System.Reflection.MethodInfo methInfo = type.GetMethod("ToString", new global::System.Type[] {
                            iConvertibleType});
                if ((methInfo != null)) {
                    return ((string)(methInfo.Invoke(objectToConvert, new object[] {
                                this.formatProvider})));
                }
                return objectToConvert.ToString();
            }
        }
    }
}
