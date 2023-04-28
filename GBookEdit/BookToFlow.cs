using System;
using System.Windows.Documents;
using System.Xml;

namespace GBookEdit.WPF
{
    internal class BookToFlow
    {
        internal static FlowDocument Load(XmlReader reader)
        {
            return new FlowDocument();
        }
    }
}