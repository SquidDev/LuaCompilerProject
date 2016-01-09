using System;
using System.IO;
using System.Text;
using System.Globalization;

namespace LuaCP.CodeGen {
    public sealed class IndentedTextWriter : TextWriter {
        private TextWriter writer;
        private int indentLevel;
        private bool tabsPending;
        private string tabString;
 
        public const string DefaultTabString = "    ";

        public IndentedTextWriter(TextWriter writer, string tabString = DefaultTabString): base(CultureInfo.InvariantCulture) {
            this.writer = writer;
            this.tabString = tabString;
            indentLevel = 0;
            tabsPending = false;
        }

        public override Encoding Encoding { get { return writer.Encoding; } }

        public override string NewLine {
            get { return writer.NewLine; }
 
            set { writer.NewLine = value; }
        }
 
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the number of spaces to indent.
        ///    </para>
        /// </devdoc>
        public int Indent {
            get {
                return indentLevel;
            }
            set {
                if (value < 0) value = 0;
                indentLevel = value;
            }
        }

        public TextWriter InnerWriter { get { return writer; } }
 
        protected override void Dispose(bool disposing) {
            writer.Dispose();
			base.Dispose(disposing);
        }
 

        public override void Flush() {
            writer.Flush();
        }

        private void OutputTabs() {
            if (tabsPending) {
                for (int i=0; i < indentLevel; i++) {
                    writer.Write(tabString);
                }
                tabsPending = false;
            }
        }

        public override void Write(string s) {
            OutputTabs();
            writer.Write(s);
        }

        public override void Write(bool value) {
            OutputTabs();
            writer.Write(value);
        }

        public override void Write(char value) {
            OutputTabs();
            writer.Write(value);
        }

        public override void Write(char[] buffer) {
            OutputTabs();
            writer.Write(buffer);
        }

        public override void Write(char[] buffer, int index, int count) {
            OutputTabs();
            writer.Write(buffer, index, count);
        }

        public override void Write(double value) {
            OutputTabs();
            writer.Write(value);
        }

        public override void Write(float value) {
            OutputTabs();
            writer.Write(value);
        }

        public override void Write(int value) {
            OutputTabs();
            writer.Write(value);
        }

        public override void Write(long value) {
            OutputTabs();
            writer.Write(value);
        }

        public override void Write(object value) {
            OutputTabs();
            writer.Write(value);
        }

        public override void Write(string format, object arg0) {
            OutputTabs();
            writer.Write(format, arg0);
        }

        public override void Write(string format, object arg0, object arg1) {
            OutputTabs();
            writer.Write(format, arg0, arg1);
        }

        public override void Write(string format, params object[] arg) {
            OutputTabs();
            writer.Write(format, arg);
        }

        public override void WriteLine(string s) {
            OutputTabs();
            writer.WriteLine(s);
            tabsPending = true;
        }

        public override void WriteLine() {
            writer.WriteLine();
            tabsPending = true;
        }

        public override void WriteLine(bool value) {
            OutputTabs();
            writer.WriteLine(value);
            tabsPending = true;
        }
 
        public override void WriteLine(char value) {
            OutputTabs();
            writer.WriteLine(value);
            tabsPending = true;
        }

        public override void WriteLine(char[] buffer) {
            if(buffer != null && buffer.Length > 0) OutputTabs();
            writer.WriteLine(buffer);
            tabsPending = true;
        }

        public override void WriteLine(char[] buffer, int index, int count) {
            if(count > 0) OutputTabs();
            writer.WriteLine(buffer, index, count);
            tabsPending = true;
        }

        public override void WriteLine(double value) {
            OutputTabs();
            writer.WriteLine(value);
            tabsPending = true;
        }

        public override void WriteLine(float value) {
            OutputTabs();
            writer.WriteLine(value);
            tabsPending = true;
        }
 
        public override void WriteLine(int value) {
            OutputTabs();
            writer.WriteLine(value);
            tabsPending = true;
        }

        public override void WriteLine(long value) {
            OutputTabs();
            writer.WriteLine(value);
            tabsPending = true;
        }

        public override void WriteLine(object value) {
            OutputTabs();
            writer.WriteLine(value);
            tabsPending = true;
        }

        public override void WriteLine(string format, object arg0) {
            OutputTabs();
            writer.WriteLine(format, arg0);
            tabsPending = true;
        }

        public override void WriteLine(string format, object arg0, object arg1) {
            OutputTabs();
            writer.WriteLine(format, arg0, arg1);
            tabsPending = true;
        }

        public override void WriteLine(string format, params object[] arg) {
            OutputTabs();
            writer.WriteLine(format, arg);
            tabsPending = true;
        }

        public override void WriteLine(UInt32 value) {
            OutputTabs();
            writer.WriteLine(value);
            tabsPending = true;
        }

    }
}
