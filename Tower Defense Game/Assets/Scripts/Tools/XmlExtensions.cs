// XmlExtensions.cs
using System.Xml;

public static class XmlExtensions
{
    public static int IndexOf(this XmlNodeList nodeList, XmlNode node)
    {
        for (int i = 0; i < nodeList.Count; i++)
        {
            if (nodeList[i] == node)
            {
                return i;
            }
        }
        return -1;
    }
}