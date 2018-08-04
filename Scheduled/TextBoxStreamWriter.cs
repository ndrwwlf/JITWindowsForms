using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherServiceForm
{
    using System;
    using System.Text;
    using System.IO;
    using System.Windows.Forms;

    namespace ConsoleRedirection
    {
        public class TextBoxStreamWriter : TextWriter
        {
            RichTextBox _output = null;

            public TextBoxStreamWriter(RichTextBox output)
            {
                _output = output;
                _output.Focus();
                _output.ScrollToCaret();
                //_output.ScrollToCaret();
                //_output.SelectionStart = _output.TextLength;
                //_output.ScrollToCaret();
            }

            public override void Write(char value)
            {
                base.Write(value);
                _output.AppendText(value.ToString()); // When character data is written, append it to the text box.
            }

            public override Encoding Encoding
            {
                get { return System.Text.Encoding.UTF8; }
            }
        }
    }
}
