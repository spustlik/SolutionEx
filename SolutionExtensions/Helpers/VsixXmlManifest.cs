using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SolutionExtensions
{
    public class VsixXmlManifest
    {
        static readonly XNamespace ns = XNamespace.Get("http://schemas.microsoft.com/developer/vsx-schema/2011");
        private XDocument xml;

        public VsixXmlManifest(XDocument xmlManifest)
        {
            this.xml = xmlManifest;
        }
        public DateTime ManifestDateTime { get; private set; }
        private XElement RootElement => xml.Element(ns + "PackageManifest");
        private XElement MetadataElement => RootElement?.Element(ns + "Metadata");
        public string Version => MetadataElement?.Element(ns + "Identity")?.Attribute("Version")?.Value;
        public string Publisher => MetadataElement?.Element(ns + "Identity")?.Attribute("Publisher")?.Value;
        public string DisplayName => MetadataElement?.Element(ns + "DisplayName")?.Value;
        public string Description => MetadataElement?.Element(ns + "Description")?.Value;

        public static VsixXmlManifest Load(Package package)
        {
            if (package == null)
                return null;
            var assembly = package.GetType().Assembly;
            var location = assembly.Location;
            var manifestXmlFile = Path.Combine(Path.GetDirectoryName(location), "extension.vsixmanifest");
            if (!File.Exists(manifestXmlFile))
                return null;
            var xmlManifest = XDocument.Load(manifestXmlFile);
            if (xmlManifest == null)
                return null;
            return new VsixXmlManifest(xmlManifest)
            {
                ManifestDateTime = File.GetLastWriteTime(manifestXmlFile)
            };
        }
    }
}