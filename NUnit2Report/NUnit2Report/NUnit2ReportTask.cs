//
// NUnit2ReportTask.cs
//
// Author:
//    Gilles Bayon (gilles.bayon@laposte.net)
// Updated Author:
//    Thang Chung (email: thangchung@ymail.com, website: weblogs.asp.net/thangchung)
//
// Copyright (C) 2010 ThangChung
//

//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//

// TO DO LIST
// Regarder ds Nunit-frame les variables Nant qui sont en commentaires
// Rajouter les commentaires xml
//	Example :
// 		<nunit2report out="NUnitReport(No-Frame).html" >
//			<fileset>
//				<includes name="result.xml" />
//			</fileset>
//			<summaries>
//				<includes name="comment.xml" />
//			</summaries>
//		</nunit2report>
// Clean code

//#define ECHO_MODE

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;

namespace NAnt.NUnit2ReportTasks
{
    [TaskName("nunit2report")]
    public class NUnit2ReportTask : Task
    {
        private const string XslDefFileNoframe = "NUnit-NoFrame.xsl";
        private const string XslDefFileSummary = "NReport-Summary.xsl";

        private const string XslI18NFile = "i18n.xsl";

        private string _outFilename = "index.htm";
        private string _todir = "";

        private readonly FileSet _fileset = new FileSet();
        private XmlDocument _fileSetSummary;

        private readonly FileSet _summaries = new FileSet();
        private string _tempXmlFileSummarie = "";

        private string _xslFile = "";
        private string _i18NXsl = "";
        private string _openDescription = "no";
        private string _language = "";
        private string _format = "noframes";
        private string _nantLocation = "";
        private XsltArgumentList _xsltArgs;

        /// <summary>
        /// The format of the generated report.
        /// Must be "noframes" or "frames".
        /// Default to "noframes".
        /// </summary>
        [TaskAttribute("format", Required = false)]
        public string Format
        {
            get { return _format; }
            set { _format = value; }
        }

        /// <summary>
        /// The output language.
        /// </summary>
        [TaskAttribute("lang", Required = false)]
        public string Language
        {
            get { return _language; }
            set { _language = value; }
        }

        /// <summary>
        /// Open all description method. Default to "false".
        /// </summary>
        [TaskAttribute("opendesc", Required = false)]
        public string OpenDescription
        {
            get { return _openDescription; }
            set { _openDescription = value; }
        }

        /// <summary>
        /// The directory where the files resulting from the transformation should be written to.
        /// </summary>
        [TaskAttribute("todir", Required = false)]
        public string Todir
        {
            get { return _todir; }
            set { _todir = value; }
        }

        /// <summary>
        /// Index of the Output HTML file(s).
        /// Default to "index.htm".
        /// </summary>
        [TaskAttribute("out", Required = false)]
        public string OutFilename
        {
            get { return _outFilename; }
            set { _outFilename = value; }
        }

        /// <summary>
        /// Set of XML files to use as input
        /// </summary>
        [BuildElement("fileset")]
        public FileSet XmlFileSet
        {
            get { return _fileset; }
        }

        /// <summary>
        /// Set of XML files to use as input
        /// </summary>
        [BuildElement("summaries")]
        public FileSet XmlSummaries
        {
            get { return _summaries; }
        }

        ///<summary>
        ///Initializes task and ensures the supplied attributes are valid.
        ///</summary>
        ///<param name="taskNode">Xml node used to define this task instance.</param>
        protected override void InitializeTask(XmlNode taskNode)
        {
            var thisAssm = Assembly.GetExecutingAssembly();

#if ECHO_MODE
				Console.WriteLine ("Location : "+thisAssm.CodeBase);
#endif

            _nantLocation = Path.GetDirectoryName(thisAssm.CodeBase);//(thisAssm.Location

            if (Format == "noframes")
            {
                _xslFile = Path.Combine(_nantLocation, XslDefFileNoframe);
            }

            _i18NXsl = Path.Combine(_nantLocation, XslI18NFile);
            Path.Combine(_nantLocation, XslDefFileSummary);

            if (XmlFileSet.FileNames.Count == 0)
            {
                throw new BuildException("NUnitReport fileset cannot be empty!", Location);
            }

            foreach (var file in XmlSummaries.FileNames)
            {
                _tempXmlFileSummarie = file;
            }

            // Get the Nant, OS parameters
            _xsltArgs = GetPropertyList();

            //Create directory if ...
            if (Todir != "")
            {
                Directory.CreateDirectory(Todir);
            }
        }

        /// <summary>
        /// This is where the work is done
        /// </summary>
        protected override void ExecuteTask()
        {
            _fileSetSummary = CreateSummaryXmlDoc();

            foreach (var file in XmlFileSet.FileNames)
            {
                var source = new XmlDocument();
                source.Load(file);
                if (source.DocumentElement == null) continue;
                var node = _fileSetSummary.ImportNode(source.DocumentElement, true);
                if (_fileSetSummary.DocumentElement != null) _fileSetSummary.DocumentElement.AppendChild(node);
            }

            //
            // prepare properties and transform
            //
            try
            {
                if (Format == "noframes")
                {
                    var xslTransform = new XslTransform();

                    xslTransform.Load(_xslFile);

                    // xmlReader hold the first transformation
                    var xmlReader = xslTransform.Transform(_fileSetSummary, _xsltArgs);

                    // ---------- i18n --------------------------
                    var xsltI18NArgs = new XsltArgumentList();
                    xsltI18NArgs.AddParam("lang", "", Language);

                    var xslt = new XslTransform();
                    //Load the i18n stylesheet.
                    xslt.Load(_i18NXsl);

                    var xmlDoc = new XPathDocument(xmlReader);

                    var writerFinal = new XmlTextWriter(Path.Combine(Todir, OutFilename), System.Text.Encoding.GetEncoding("ISO-8859-1"));
                    // Apply the second transform to xmlReader to final ouput
                    xslt.Transform(xmlDoc, xsltI18NArgs, writerFinal);

                    xmlReader.Close();
                    writerFinal.Close();
                }
                else
                {
                    try
                    {
#if ECHO_MODE
							Console.WriteLine ("Initializing StringReader ...");
#endif

                        // create the index.html
                        var stream = new StringReader("<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='1.0' >" +
                                                               "<xsl:output method='html' indent='yes' encoding='ISO-8859-1'/>" +
                                                               "<xsl:include href=\"" + Path.Combine(_nantLocation, "NUnit-Frame.xsl") + "\"/>" +
                                                               "<xsl:template match=\"test-results\">" +
                                                               "   <xsl:call-template name=\"index.html\"/>" +
                                                               " </xsl:template>" +
                                                               " </xsl:stylesheet>");
                        Write(stream, Path.Combine(Todir, OutFilename));

                        // create the stylesheet.css
                        stream = new StringReader("<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='1.0' >" +
                            "<xsl:output method='html' indent='yes' encoding='ISO-8859-1'/>" +
                            "<xsl:include href=\"" + Path.Combine(_nantLocation, "NUnit-Frame.xsl") + "\"/>" +
                            "<xsl:template match=\"test-results\">" +
                            "   <xsl:call-template name=\"stylesheet.css\"/>" +
                            " </xsl:template>" +
                            " </xsl:stylesheet>");
                        Write(stream, Path.Combine(Todir, "stylesheet.css"));

                        // create the overview-summary.html at the root
                        stream = new StringReader("<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='1.0' >" +
                            "<xsl:output method='html' indent='yes' encoding='ISO-8859-1'/>" +
                            "<xsl:include href=\"" + Path.Combine(_nantLocation, "NUnit-Frame.xsl") + "\"/>" +
                            "<xsl:template match=\"test-results\">" +
                            "    <xsl:call-template name=\"overview.packages\"/>" +
                            " </xsl:template>" +
                            " </xsl:stylesheet>");
                        Write(stream, Path.Combine(Todir, "overview-summary.html"));

                        // create the allclasses-frame.html at the root
                        stream = new StringReader("<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='1.0' >" +
                            "<xsl:output method='html' indent='yes' encoding='ISO-8859-1'/>" +
                            "<xsl:include href=\"" + Path.Combine(_nantLocation, "NUnit-Frame.xsl") + "\"/>" +
                            "<xsl:template match=\"test-results\">" +
                            "    <xsl:call-template name=\"all.classes\"/>" +
                            " </xsl:template>" +
                            " </xsl:stylesheet>");
                        Write(stream, Path.Combine(Todir, "allclasses-frame.html"));

                        // create the overview-frame.html at the root
                        stream = new StringReader("<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='1.0' >" +
                            "<xsl:output method='html' indent='yes' encoding='ISO-8859-1'/>" +
                            "<xsl:include href=\"" + Path.Combine(_nantLocation, "NUnit-Frame.xsl") + "\"/>" +
                            "<xsl:template match=\"test-results\">" +
                            "    <xsl:call-template name=\"all.packages\"/>" +
                            " </xsl:template>" +
                            " </xsl:stylesheet>");
                        Write(stream, Path.Combine(Todir, "overview-frame.html"));

                        // Create directory
                        string path;

                        //--- Change 11/02/2003 -- remove
                        //XmlDocument doc = new XmlDocument();
                        //doc.Load("result.xml"); _FileSetSummary
                        //---
                        var xpathNavigator = _fileSetSummary.CreateNavigator(); //doc.CreateNavigator();

                        // Get All the test suite containing test-case.
                        if (xpathNavigator != null)
                        {
                            var expr = xpathNavigator.Compile("//test-suite[(child::results/test-case)]");

                            var iterator = xpathNavigator.Select(expr);
                            string directory;

                            while (iterator.MoveNext())
                            {
                                var xpathNavigator2 = iterator.Current;
                                var testSuiteName = iterator.Current.GetAttribute("name", "");

#if ECHO_MODE
									Console.WriteLine("Test case : "+testSuiteName);
#endif

                                // Get get the path for the current test-suite.
                                var iterator2 = xpathNavigator2.SelectAncestors("", "", true);
                                path = "";
                                var parent = "";
                                var parentIndex = -1;

                                while (iterator2.MoveNext())
                                {
                                    directory = iterator2.Current.GetAttribute("name", "");
                                    if (directory != "" && directory.IndexOf(".dll") < 0)
                                    {
                                        path = directory + "/" + path;
                                    }
                                    if (parentIndex == 1)
                                        parent = directory;
                                    parentIndex++;
                                }
                                Directory.CreateDirectory(Path.Combine(Todir, path));// path = xx/yy/zz

#if ECHO_MODE
									Console.WriteLine("path="+path+"\n");
#endif

#if ECHO_MODE
									Console.WriteLine("parent="+parent+"\n");
#endif

                                // Build the "testSuiteName".html file
                                // Correct MockError duplicate testName !
                                // test-suite[@name='MockTestFixture' and ancestor::test-suite[@name='Assemblies'][position()=last()]]

                                stream = new StringReader("<xsl:stylesheet xmlns:xsl='http://www.w3.org/1999/XSL/Transform' version='1.0' >" +
                                                          "<xsl:output method='html' indent='yes' encoding='ISO-8859-1'/>" +
                                                          "<xsl:include href=\"" + Path.Combine(_nantLocation, "NUnit-Frame.xsl") + "\"/>" +
                                                          "<xsl:template match=\"/\">" +
                                                          "	<xsl:for-each select=\"//test-suite[@name='" + testSuiteName + "' and ancestor::test-suite[@name='" + parent + "'][position()=last()]]\">" +
                                                          "		<xsl:call-template name=\"test-case\">" +
                                                          "			<xsl:with-param name=\"dir.test\">" + String.Join(".", path.Split('/')) + "</xsl:with-param>" +
                                                          "		</xsl:call-template>" +
                                                          "	</xsl:for-each>" +
                                                          " </xsl:template>" +
                                                          " </xsl:stylesheet>");
                                Write(stream, Path.Combine(Path.Combine(Todir, path), testSuiteName + ".html"));

#if ECHO_MODE
									Console.WriteLine("dir="+ Todir+path+" Generate "+testSuiteName+".html\n");
#endif
                            }
                        }

#if ECHO_MODE
								Console.WriteLine ("Processing ...");
								Console.WriteLine ();
#endif
                    }

                    catch (Exception e)
                    {
                        Console.WriteLine("Exception: {0}", e);
                    }
                }
            }
            catch (Exception e)
            {
                throw new BuildException(e.Message, Location);
            }
        }

        /// <summary>
        /// Initializes the XmlDocument instance
        /// used to summarize the test results
        /// </summary>
        /// <returns></returns>
        private static XmlDocument CreateSummaryXmlDoc()
        {
            var doc = new XmlDocument();
            var root = doc.CreateElement("testsummary");
            root.SetAttribute("created", DateTime.Now.ToString());
            doc.AppendChild(root);

            return doc;
        }

        /// <summary>
        /// Builds an XsltArgumentList with all
        /// the properties defined in the
        /// current project as XSLT parameters.
        /// </summary>
        /// <returns></returns>
        private XsltArgumentList GetPropertyList()
        {
            var args = new XsltArgumentList();

#if ECHO_MODE
		Console.WriteLine();
		Console.WriteLine("XsltArgumentList");
#endif

            foreach (var entry in
                Project.Properties.Cast<DictionaryEntry>().Where(entry => (string)entry.Value != null))
            {
                try
                {
                    args.AddParam((string)entry.Key, "", entry.Value);
                }
                catch (ArgumentException aex)
                {
                    Console.WriteLine("Invalid Xslt parameter {0}", aex);
                }
            }

            // Add argument to the C# XML comment file
            args.AddParam("summary.xml", "", _tempXmlFileSummarie);
            // Add open.description argument
            args.AddParam("open.description", "", OpenDescription);

            return args;
        }

        private void Write(TextReader stream, string fileName)
        {
            // Load the XmlTextReader from the stream
            var reader = new XmlTextReader(stream);
            var xslTransform = new XslTransform();
            //Load the stylesheet from the stream.
            xslTransform.Load(reader);

            //xmlDoc = new XPathDocument("result.xml");

            // xmlReader hold the first transformation
            var xmlReader = xslTransform.Transform(_fileSetSummary, _xsltArgs);//(xmlDoc, _xsltArgs);

            // ---------- i18n --------------------------
            var xsltI18NArgs = new XsltArgumentList();
            xsltI18NArgs.AddParam("lang", "", Language);

            var xslt = new XslTransform();

            //Load the stylesheet.
            xslt.Load(_i18NXsl);

            var xmlDoc = new XPathDocument(xmlReader);

            var writerFinal = new XmlTextWriter(fileName, System.Text.Encoding.GetEncoding("ISO-8859-1"));
            // Apply the second transform to xmlReader to final ouput
            xslt.Transform(xmlDoc, xsltI18NArgs, writerFinal);

            xmlReader.Close();
            writerFinal.Close();
        }
    } // class NUnit2ReportTask
} // namespace  NAnt.NUnit2ReportTasks