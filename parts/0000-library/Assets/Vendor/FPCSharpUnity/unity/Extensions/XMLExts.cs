﻿using System.Xml;

namespace FPCSharpUnity.unity.Extensions {
  public static class XMLExts {
    public static XmlElement setInnerText(this XmlElement xml, string text) {
      xml.InnerText = text;
      return xml;
    }

    public static XmlElement textElem(this XmlDocument doc, string name, string text)
    { return doc.CreateElement(name).setInnerText(text); }
  }
}
