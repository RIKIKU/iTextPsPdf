﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Collections.ObjectModel;
using System.Reflection;

namespace iTextPsPdf
{
    /// <summary>
    /// <para type="synopsis">Used to Export text to a PDF File.</para>
    /// <para type="description">This cmdlet is used to export a string as a PDF file.</para>
    /// </summary>
    /// <example>
    ///     <code>Get-ChildItem -Recurse | convertto-json | Export-pdf -Path "C:\json.pdf" -PageSize A3 -FlipOrientation</code>
    ///     <para>In this example, we use Get-ChildItem -Recuse to generate some data, we pipe the data into convertto-json which outputs a string of formatted JSON, then we pipe that into Export-pdf which saves that string as a PDF and creates each page as A3 Landscape.</para>
    /// </example>   
    [Cmdlet(VerbsData.Export, "PDF", SupportsShouldProcess = false)]
    public class ExportPdf : Cmdlet, IDynamicParameters
    {

        /// <summary>
        /// <para type="description">Specifies the path to the PDF output file. This parameter is required.</para>
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 0
            )]
        public string Path
        {
            get;
            set;
        }

        private static RuntimeDefinedParameterDictionary _staticStorage;
        //https://msdn.microsoft.com/en-us/library/bb336630(v=vs.110).aspx
        /// <summary>
        /// <para type="description">The paper size. Portrate unless otherwise specified.</para>
        /// </summary>
        /// <returns></returns>
        public object GetDynamicParameters()
        {
            IEnumerable<string> pageSizes = typeof(PageSize).GetFields().Select(x => x.Name);
            var runtimeDefinedParameterDictionary = new RuntimeDefinedParameterDictionary();
            var attrib = new Collection<Attribute>()
            {
                new ParameterAttribute() {
                    HelpMessage = "A Slack Emoji to use as the avatar",
                    Mandatory = true,
                    Position = 1
                },
                new ValidateSetAttribute(pageSizes.ToArray())
            };
            var parameter = new RuntimeDefinedParameter("PageSize", typeof(String), attrib);
            runtimeDefinedParameterDictionary.Add("PageSize", parameter);
            _staticStorage = runtimeDefinedParameterDictionary;
            return runtimeDefinedParameterDictionary;
        }

        
        /// <summary>
        /// <para type="description">Changes the page orientation from portrate to landscape and vice versa.</para>
        /// </summary>
        [Parameter(
            Mandatory = false,
            Position = 2
            )]
        public SwitchParameter FlipOrientation { get; set; }

        /// <summary>
        /// <para type="description">Specifies the string to export as a PDF. Enter a variable that contains the string or type a command or expression that gets the objects. You can also pipe objects to Export-PDF</para>
        /// </summary>
        [Parameter(
            Mandatory = true,
            ValueFromPipeline = true,
            Position = 3
            )]
        public string Input
        {
            get;
            set;
        }
        /// <summary>
        /// <para type="description">Overwrites the file if it exists</para>
        /// </summary>
        [Parameter(
            Mandatory = false
            )]
        public SwitchParameter Force
        {
            get;
            set;
        }
        private Document doc;
        private FileStream fs;
        private PdfWriter writer;
        protected override void BeginProcessing()
        {
            if (Force)
            {
                fs = new FileStream(Path, FileMode.Create, FileAccess.Write, FileShare.None);
            }
            else
            {
                fs = new FileStream(Path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            }

            #region SetPageSize
            var PageSizeRuntime = new RuntimeDefinedParameter();
            _staticStorage.TryGetValue("PageSize", out PageSizeRuntime);
            WriteDebug("setting PageSize property info");

            FieldInfo pageSizeProperty = typeof(PageSize).GetField(PageSizeRuntime.Value.ToString());
            WriteDebug("Selecting PageSize");
            Rectangle rectangle = (Rectangle)pageSizeProperty.GetValue(null);
            #endregion


            #region SetPageOrientation
            if (FlipOrientation)
            {
                //flips the orientation of the page.
                WriteDebug("Fliping page dimensions and instantiating document");
                doc = new Document(rectangle.Rotate());
            }
            else
            {
                WriteDebug("instantiating document");
                doc = new Document(rectangle);
            }
            #endregion
            writer = PdfWriter.GetInstance(doc, fs);
            WriteDebug("stamping document with creation time and author");
            doc.AddCreationDate();
            doc.AddAuthor(Environment.UserName);
            WriteDebug("Opening Document");
            doc.Open();
            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            //add the text to the document.
            WriteDebug("adding string to document");
            doc.Add(new Paragraph(Input));
            
            /*
            PdfReader reader = new PdfReader("Chapter1_Example1.pdf");
            string text = string.Empty;
            for (int page = 1; page <= reader.NumberOfPages; page++)
            {
                text += PdfTextExtractor.GetTextFromPage(reader, page);
            }
            reader.Close();
            */
            base.ProcessRecord();
        }

        protected override void EndProcessing()
        {
            WriteDebug("Closing document");
            doc.Close();
            base.EndProcessing();
        }
    }
}
