using SolutionExtensions.Model;
using SolutionExtensions.Reflector;
using System.Security.Permissions;

namespace SolutionExtensions.UI
{
    public static class SampleData
    {
        public static ExtensionItem[] Extensions => new[]
        {
            new ExtensionItem()
            {
                Title= "Item 1",
                ClassName="MyClass1",
                DllPath="MyDll",
            },
            new ExtensionItem()
            {
                Title= "Item 2",
                ClassName="MyClass2",
                DllPath="MyDll2.dll",
                ShortCutKey="CTRL+G",
                ArgumentTitle="Enter number",
                Argument="42",
                CompileBeforeRun=true,
                OutOfProcess=true,
            },
            new ExtensionItem()
            {
                Title= "Item 3",
                ClassName="MyGen",
                DllPath="MyDll",
                IsGenerator=true,
                Files = {
                    "File1.xml",
                    "File2.cs"
                }
            },
            new ExtensionItem()
            {
                Title= "More",
                ClassName="MyClass1",
                DllPath="MyDll",
            },
        };

        public static ReflectorVM ReflectorVM => new ReflectorVM()
        {
            Children =
            {
                new ReflectorRoot()
                {
                    RootType="Test",
                    IsExpanded=true,
                    CanExpandEnumerable=true,
                        CanExpandInterfaces=true,
                        CanExpandMethods=true,
                        CanExpandProperties=true,
                    Children =
                    {
                        new ReflectorMethod()
                        {
                            MethodName="MyMethod",
                            Signature="(string x)"
                        },
                        new ReflectorPropertyValue()
                        {
                            PropertyName="MyProperty",
                            ValueSimpleText="42"
                        }

                    }
                }
            }
        };

    }
}
