using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SystemsGarden.mc2.RemoteConnector.Handlers.PayrollIntegrationHandlerServer
{


    /// <summary>
    /// Class to generate simple calculation functions *,/,+,- from DataTree (config.tree)
    /// </summary>
    internal static class CodeDomCalculationParser
    {
        public static CompilerResults CompiledResult;
        private static MethodInfo method;


        /// <summary>
        /// Returns double value from expression calculation
        /// </summary>
        /// <param name="expression">expression to calculate eg. 1000 / 60 / 100</param>
        /// <param name="value">Variable to use with calculation eg. Hours</param>
        /// <param name="precedingOperator">Operator between value and expression *,/,+ or - /</param>
        /// <returns>Computed value with value and expression eg. Hours / 1000 / 60 / 60
        /// <para>eg. Would return milliseconds to hours</para>
        /// </returns>
        public static double EvaluateExpressionAndReturnValue(string expression, double value, string precedingOperator)
        {
            // Note: Use "{{" to denote a single "{"
            string code = string.Format(
                @"using System;
                public static class CodeDomCalculator
                {{ 
                    public static double Calculate(double value, string precedingOperator, string expression)
                    {{
                        double val = value;
                        val = val {0} {1};
                        return val;
                    }}
                }}", precedingOperator, expression);

            if (CompiledResult == null)
            {
                CompiledResult = CompileScript(code);

                if (CompiledResult.Errors.HasErrors)
                {
                    throw new InvalidOperationException("Expression has a syntax error.");
                }

                Assembly assembly = CompiledResult.CompiledAssembly;
                method = assembly.GetType("CodeDomCalculator").GetMethod("Calculate");
            }
            double result = 0;
            try
            {
                //method.Invoke(null for constructor, new object[] for parameters for code "public static double Calculate(double value, string precedingOperator, string expression)")
                result = (double)method.Invoke(null, new object[] { value, precedingOperator, expression });

            }
            catch (Exception ex)
            {
                throw new Exception("Expression is invalid: '" + expression + "' because " + ex.Message);
            }
            return result;
        }

        /// <summary>
        /// Actual compilation
        /// </summary>
        /// <param name="code">Any C# code you wish to compile, need 1 or more types and their members</param>
        /// <returns></returns>
        private static CompilerResults CompileScript(string code)
        {
            CompilerParameters parms = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true,
                IncludeDebugInformation = false
            };

            CodeDomProvider compiler = CodeDomProvider.CreateProvider("CSharp");

            return compiler.CompileAssemblyFromSource(parms, code);
            
        }
    }
}
