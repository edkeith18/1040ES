namespace _1040ES
{
    using iText.Forms;
    using iText.Kernel.Pdf;
    using iText.Kernel.Utils;

    internal class Program
    {

        // These are the fields in the AcroForm on Form1040ES
        public const string BOX01 = "topmostSubform[0].Page12[0].f12_1[0]";
        public const string BOX02a = "topmostSubform[0].Page12[0].f12_2[0]";
        public const string BOX02b = "topmostSubform[0].Page12[0].f12_3[0]";
        public const string BOX02c = "topmostSubform[0].Page12[0].f12_4[0]";
        public const string BOX02d = "topmostSubform[0].Page12[0].f12_5[0]";
        public const string BOX03 = "topmostSubform[0].Page12[0].f12_6[0]";
        public const string BOX04 = "topmostSubform[0].Page12[0].f12_7[0]";
        public const string BOX05 = "topmostSubform[0].Page12[0].f12_8[0]";
        public const string BOX06 = "topmostSubform[0].Page12[0].f12_9[0]";
        public const string BOX07 = "topmostSubform[0].Page12[0].f12_10[0]";
        public const string BOX08 = "topmostSubform[0].Page12[0].f12_11[0]";
        public const string BOX09 = "topmostSubform[0].Page12[0].f12_12[0]";
        public const string BOX10 = "topmostSubform[0].Page12[0].f12_13[0]";
        public const string BOX11a = "topmostSubform[0].Page12[0].f12_14[0]";
        public const string BOX11b = "topmostSubform[0].Page12[0].f12_15[0]";
        public const string BOX11c = "topmostSubform[0].Page12[0].f12_16[0]";
        public const string BOX12a = "topmostSubform[0].Page12[0].f12_17[0]";
        public const string BOX12b = "topmostSubform[0].Page12[0].f12_18[0]";
        public const string BOX12c = "topmostSubform[0].Page12[0].f12_19[0]";
        public const string BOX13 = "topmostSubform[0].Page12[0].f12_20[0]";
        public const string BOX14a = "topmostSubform[0].Page12[0].f12_21[0]";
        public const string BOX14aCheckYes = "topmostSubform[0].Page12[0].c12_1[0]";
        public const string BOX14aCheckNo = "topmostSubform[0].Page12[0].c12_1[1]";
        public const string BOX14b = "topmostSubform[0].Page12[0].f12_22[0]";
        public const string BOX14bCheckYes = "topmostSubform[0].Page12[0].c12_2[0]";
        public const string BOX14bCheckNo = "topmostSubform[0].Page12[0].c12_2[1]";

        static void Main(string[] args)
        {

            // We need 9 values to be input
            int expectedArgCount = 9;
            if (args.Length != expectedArgCount)
            {
                DebugPause($"{args.Length} args were passed in, but {expectedArgCount} were expected.");
                Usage();
            }

            // This is the PDF template and filled-in version of form
            string form1040ES_template = args[0];
            string form1040ES_filled = args[1];

            //Uncomment this to extract a page from a PDF
            //Sorry that this is such a hack, future Ed, but I don't want to make a whole new project for this, nor overload the argument line for this program
            //ExtractPDFPage(form1040ES_template, form1040ES_filled, 12);

            // This is the data for filling in the form
            int box01Value = 0;
            int box02aValue = 0;
            int box04Value = 0;
            int box05Value = 0;
            int box10Value = 0;
            int priorYearAGI = 0;
            int priorYearTaxes = 0;
            try
            {
                box01Value = Int32.Parse(args[2]);  // AGI
                box02aValue = Int32.Parse(args[3]); // Deductions
                box04Value = Int32.Parse(args[4]);  // Standard tax
                box05Value = Int32.Parse(args[5]);  // Alternative Minimum tax
                box10Value = Int32.Parse(args[6]);  // Other taxes (ie, NIIT)
                priorYearAGI = Int32.Parse(args[7]);  // Prior Year AGI
                priorYearTaxes = Int32.Parse(args[8]);  // Prior Year Taxes
            }
            catch
            {
                Usage();
            }

            // This data is largely invariant, though box12b changes yearly.  For now, just hardcode these.
            int box02bValue = 0;        // QBID
            int box02cValue = 0;        // Schedule 1-A deductions
            int box07Value = 0;         // Credits
            int box09Value = 0;         // Self-employment tax
            int box11bValue = 0;        // More credits (earned income, aaoc, etc)
            int box13Value = 0;         // Income tax withheld

            // This data is derived from the input data
            int box02dValue = box02aValue + box02bValue + box02cValue;
            int box03Value = box01Value - box02dValue;
            int box06Value = box04Value + box05Value;
            int box08Value = Math.Max(box06Value - box07Value, 0); // minimum value is 0
            int box11aValue = box08Value + box09Value + box10Value;
            int box11cValue = box11aValue - box11bValue;
            int box12aValue = (int)Math.Round(box11cValue * .9, 0);
            int box12bValue = (int)Math.Round(priorYearTaxes * (priorYearAGI > 150000 ? 1.1 : 1.0));    // Estimated payment based on prior year's AGI
            int box12cValue = Math.Min(box12aValue, box12bValue);
            int box14aValue = box12cValue - box13Value;
            bool check14aYesValue = (box14aValue < 0) ? true : false;
            bool check14aNoValue = !check14aYesValue;
            int box14bValue = Math.Max(0, box11cValue - box13Value);
            bool check14bYesValue = (box14bValue < 1000) ? true : false;
            bool check14bNoValue = !check14bYesValue;

            // Open template in stamping mode.  Values get stamped on to the 2nd file passed in, the 1st file is not changed.
            // Filled-in version is overwritten if it already exists.
            PdfDocument? doc = null;
            try
            {
                doc = new PdfDocument(new PdfReader(form1040ES_template), new PdfWriter(form1040ES_filled));
            }
            catch (Exception ex)
            {
                DebugPause(ex.Message);
            }

            // Open AcroForm on document
            PdfAcroForm form = PdfAcroForm.GetAcroForm(doc, false);

            // Fill in values
            form.GetField(BOX01).SetValue(String.Format("{0:n0}", box01Value));
            form.GetField(BOX02a).SetValue(String.Format("{0:n0}", box02aValue));
            form.GetField(BOX02b).SetValue(String.Format("{0:n0}", box02bValue));
            form.GetField(BOX02c).SetValue(String.Format("{0:n0}", box02cValue));
            form.GetField(BOX02d).SetValue(String.Format("{0:n0}", box02dValue));
            form.GetField(BOX03).SetValue(String.Format("{0:n0}", box03Value));
            form.GetField(BOX04).SetValue(String.Format("{0:n0}", box04Value));
            form.GetField(BOX05).SetValue(String.Format("{0:n0}", box05Value));
            form.GetField(BOX06).SetValue(String.Format("{0:n0}", box06Value));
            form.GetField(BOX07).SetValue(String.Format("{0:n0}", box07Value));
            form.GetField(BOX08).SetValue(String.Format("{0:n0}", box08Value));
            form.GetField(BOX09).SetValue(String.Format("{0:n0}", box09Value));
            form.GetField(BOX10).SetValue(String.Format("{0:n0}", box10Value));
            form.GetField(BOX11a).SetValue(String.Format("{0:n0}", box11aValue));
            form.GetField(BOX11b).SetValue(String.Format("{0:n0}", box11bValue));
            form.GetField(BOX11c).SetValue(String.Format("{0:n0}", box11cValue));
            form.GetField(BOX12a).SetValue(String.Format("{0:n0}", box12aValue));
            form.GetField(BOX12b).SetValue(String.Format("{0:n0}", box12bValue));
            form.GetField(BOX12c).SetValue(String.Format("{0:n0}", box12cValue));
            form.GetField(BOX13).SetValue(String.Format("{0:n0}", box13Value));
            form.GetField(BOX14a).SetValue(String.Format("{0:n0}", box14aValue));
            form.GetField(BOX14aCheckYes).SetValue("0", check14aYesValue);
            form.GetField(BOX14aCheckNo).SetValue("0", check14aNoValue);

            // These only get filled in if "Yes" is checked for box 14a
            if (check14aNoValue)
            {
                form.GetField(BOX14b).SetValue(String.Format("{0:n0}", box14bValue));
                form.GetField(BOX14bCheckYes).SetValue("0", check14bYesValue);
                form.GetField(BOX14bCheckNo).SetValue("0", check14bNoValue);
            }

            doc?.Close();

        }

        static void ExtractPDFPage(string sourcePDFPath, string destPDFPath,int pageNumber)
        {

            using var sourcePDFDoc = new PdfDocument(new PdfReader(sourcePDFPath));
            using var destPDFDoc = new PdfDocument(new PdfWriter(destPDFPath));

            var copier = new PdfPageFormCopier();          // keeps AcroForm fields when copying pages
            sourcePDFDoc.CopyPagesTo(pageNumber, pageNumber, destPDFDoc, copier);          // page numbers are 1-based (so 12 is page 12)

            destPDFDoc.Close();                                   // optional (using will also close)
            sourcePDFDoc.Close();

            Environment.Exit(0);
        }

        static void Usage()
        {

            Console.WriteLine("Usage:");
            Console.WriteLine("1040ES inputFile ouputFile AGI Deductions StandardTax AMT NIIT PriorYearTaxes EstimatedTaxesPaid");
            Console.WriteLine("Example: \"d:/users/edkei/onedrive/filing cabinet/planning/Form1040ES.pdf\" \"d:/users/edkei/onedrive/desktop/2024-08 - Form1040ES.pdf\" 263520 95906 26534 0 514 25316 24452");
            Environment.Exit(1);

        }
        static void DebugPause(string message = "")
        {

            Console.WriteLine(message);
            Console.WriteLine("press a key to continue");
            Console.ReadLine();

        }


    }
}
