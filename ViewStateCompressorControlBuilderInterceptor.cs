using System;
using System.CodeDom;
using System.ComponentModel;
using System.Linq;
using System.Web.Compilation;
using System.Web.UI;

namespace ViewStateCompressor
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ViewStateCompressorControlBuilderInterceptor : ControlBuilderInterceptor
    {
        public override void OnProcessGeneratedCode(ControlBuilder controlBuilder, CodeCompileUnit codeCompileUnit, CodeTypeDeclaration baseType, CodeTypeDeclaration derivedType, CodeMemberMethod buildMethod, CodeMemberMethod dataBindingMethod, System.Collections.IDictionary additionalState)
        {
            // only run this once during page compilation, and only use this one builder (so that we don't get master pages, etc.)
            if (controlBuilder.GetType() == typeof(FileLevelPageControlBuilder))
            {
                // the page will only contain one namespace and one type
                var ns = codeCompileUnit.Namespaces.Cast<CodeNamespace>().FirstOrDefault();
                if (ns != null)
                {
                    var type = ns.Types.Cast<CodeTypeDeclaration>().FirstOrDefault();
                    if (type != null)
                    {
                        /* When this is output, it will inject this into every page:
                         * 
                         * protected override PageStatePersister PageStatePersister {
                         *   get { return new CompressedHiddenFieldPageStatePersister(this); }
                         * }
                         * 
                         */
                        CodeMemberProperty property = new CodeMemberProperty()
                        {
                            Name = "PageStatePersister",
                            HasGet = true,
                            Attributes = MemberAttributes.Override | MemberAttributes.Family,
                            Type = new CodeTypeReference(typeof(PageStatePersister))
                        };
                        var newObj = new CodeObjectCreateExpression(typeof(CompressedHiddenFieldPageStatePersister), new CodeThisReferenceExpression());
                        property.GetStatements.Add(new CodeMethodReturnStatement(newObj));
                        type.Members.Add(property);
                    }
                }
            }

            base.OnProcessGeneratedCode(controlBuilder, codeCompileUnit, baseType, derivedType, buildMethod, dataBindingMethod, additionalState);
        }
    }
}
