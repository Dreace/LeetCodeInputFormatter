// https://github.com/xiong-ang/CShape_SLN/blob/master/ClipboardDemo/ClipboardProcesser.cs
using System;
using System.IO;
using System.Windows.Forms;

namespace LeetCodeInputFormatter
{
    class ClipboardDataFormat
    {
        public static readonly string TEXT = "Text";
        public static readonly string IMAGE = "Image";
        public static readonly string FILEDROP = "FileDrop";
        public static readonly string AUDIO = "Audio";
        public static readonly string USERDEFINED = "UserDefined";
    }

    class ClipboardProcesser
    {
        public static void SetDataToClipboard(object o, string type)
        {
            try
            {
                if (o != null)
                {
                    if (type == ClipboardDataFormat.TEXT)
                    {
                        Clipboard.SetText((string)o);
                    }
                    if (type == ClipboardDataFormat.FILEDROP)
                    {
                        Clipboard.SetFileDropList((System.Collections.Specialized.StringCollection)o);
                    }
                    if (type == ClipboardDataFormat.IMAGE)
                    {
                        Clipboard.SetImage((System.Drawing.Image)o);
                    }
                    if (type == ClipboardDataFormat.AUDIO)
                    {
                        Clipboard.SetAudio((Stream)o);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public static object GetDataFromClipboardByType(string type)
        {
            object retObj = null;
            try
            {
                if (type == ClipboardDataFormat.TEXT)
                {
                    retObj = Clipboard.GetText();
                }
                if (type == ClipboardDataFormat.FILEDROP)
                {
                    retObj = Clipboard.GetFileDropList();
                }
                if (type == ClipboardDataFormat.IMAGE)
                {
                    retObj = Clipboard.GetImage();
                }
                if (type == ClipboardDataFormat.AUDIO)
                {
                    retObj = Clipboard.GetAudioStream();
                }
            }
            catch (Exception)
            {
                retObj = null;
            }
            return retObj;
        }

        public static string GetDataTypeFromClipboard()
        {
            string type = string.Empty;

            try
            {
                if (Clipboard.ContainsText())
                {
                    type = ClipboardDataFormat.TEXT;
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    type = ClipboardDataFormat.FILEDROP;
                }
                else if (Clipboard.ContainsImage())
                {
                    type = ClipboardDataFormat.IMAGE;
                }
                else if (Clipboard.ContainsAudio())
                {
                    type = ClipboardDataFormat.AUDIO;
                }
                else
                {
                    IDataObject iData = Clipboard.GetDataObject();
                    if (iData.GetDataPresent(ClipboardDataFormat.USERDEFINED))
                    {
                        type = ClipboardDataFormat.USERDEFINED;
                    }
                }
            }
            catch (Exception)
            {
            }
            return type;
        }
    }
}
