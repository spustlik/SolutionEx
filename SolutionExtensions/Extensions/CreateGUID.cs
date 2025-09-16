using EnvDTE;
using System;
using System.ComponentModel;
using System.Windows;

namespace SolutionExtensions.Extensions
{
    public class CreateGUID
    {
        [Description("Copy new GUID to clipboard")]
        public void Run(DTE DTE)
        {
            Clipboard.SetText(Guid.NewGuid().ToString("B"));
        }
    }
}
