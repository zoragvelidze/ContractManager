using Microsoft.LightSwitch.Security.Server;
using Microsoft.LightSwitch;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Web;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Globalization;
using System.ServiceModel.Syndication;
using System.Xml;
using System.Diagnostics;

namespace LightSwitchApplication
{
    public partial class ApplicationDataService
    {
        partial void ActualPayments_Inserted(ActualPayment entity)
        {
            updatePaymentSchedule(entity.ProjectId);
            updateProjectStages(entity.ProjectId);
        }

        public void updatePaymentSchedule(int ProjcetId)
        {
            decimal totalPaid = 0;

            foreach (ActualPayment actPaym in ActualPayments.Where(e => e.Project.Id == ProjcetId))
            {
                totalPaid += (actPaym.Paid ?? 0) + (actPaym.Reversal ?? 0) + (actPaym.Discount ?? 0);
            }

            decimal paidBalance = totalPaid;

            var dw = this.Application.CreateDataWorkspace();
            var paymSchedules = from ps in PaymentSchedules
                                where ps.Project.Id == ProjcetId
                                orderby ps.DueDate
                                select ps;
            foreach (PaymentSchedule paySche in paymSchedules)
            {
                decimal lineDue = paySche.TotalDue;
                decimal linePaid = Math.Min(lineDue, paidBalance);
                paySche.ClientChange = false;
                paySche.PaidTotal = linePaid;
                paidBalance -= linePaid;

                if (paySche.DueDate < DateTime.Today)
                    paySche.OverDue = paySche.TotalDue - (paySche.PaidTotal ?? 0);
                else
                    paySche.OverDue = 0;

                if (paySche.OverDue > 0)
                    paySche.Project.AlertType = 1;
                else
                    paySche.Project.AlertType = 0;
            }

            dw.ApplicationData.SaveChanges();

        }

        struct payments
        {
            public DateTime paymentDate;
            public decimal paymentAmount;
            public decimal paymentRate;
        }


        public void updateProjectStages(int projectId)
        {
            var dw = this.Application.CreateDataWorkspace();

            //var projectStages = from ProjectStage ps in ProjectStages
            //                    join CurrencyRate cr in CurrencyRates on new { closeDate = ps.CloseDate, projectCurrency = ps.Project.Currency } equals new { closeDate = cr.RateDate, projectCurrency = cr.Currency }
            //                    where (ps.Project.Id == projectId && ps.CloseDate.HasValue)
            //                    orderby ps.CloseDate
            //                    select new { projectStage = ps, currencyRate = cr.Rate };

            //select ps;

            Project project = dw.ApplicationData.Projects_SingleOrDefault(projectId);

            Currency gel = dw.ApplicationData.Currencies_GetGEL().SingleOrDefault();

            if (project.Currency.Id == gel.Id)
            {
                var gelProjcetStages = from ProjectStage ps in ProjectStages.Where(x => x.Project.Id == projectId)
                                       select ps;
                foreach (var proStage in gelProjcetStages)
                {
                    proStage.ClientChange = false;
                    if (proStage.Closed)
                    {
                        proStage.TotalGel = proStage.StageTotal;
                    }
                    else
                        proStage.TotalGel = 0;
                }
            }
            else
            {

                var actualPayments = from ActualPayment ap in ActualPayments.Where(x => x.Project.Id == projectId).OrderBy(x => x.PaymentDate)
                                     from CurrencyRate cr in CurrencyRates.Where(x => ap.PaymentDate == x.RateDate && ap.Project.Currency.Id == x.Currency.Id)
                                     select new { actualPayment = ap, currencyRate = cr.Rate };


                int actPayCount = actualPayments.Count();
                payments[] payArray = new payments[actPayCount];

                int i = 0;

                foreach (var actPayment in actualPayments)
                {
                    payArray[i].paymentDate = actPayment.actualPayment.PaymentDate;
                    payArray[i].paymentAmount = actPayment.actualPayment.Paid ?? 0;
                    payArray[i].paymentRate = actPayment.currencyRate;
                    i++;
                }



                var projcetStages = from ProjectStage ps in ProjectStages.Where(x => x.Project.Id == projectId).OrderBy(x => x.CloseDate)
                                    from CurrencyRate cr in CurrencyRates.Where(x => ps.CloseDate == x.RateDate && ps.Project.Currency.Id == x.Currency.Id)
                                    select new { projectStage = ps, currencyRate = cr.Rate };

                foreach (var proStage in projcetStages)
                {
                    proStage.projectStage.ClientChange = false;
                    if (proStage.projectStage.Closed)
                    {
                        decimal totalAdvanceCurr = 0;
                        decimal totalAdvanceGel = 0;
                        decimal remStage = proStage.projectStage.StageTotal;

                        for (i = 0; i < actPayCount; i++)
                        {
                            decimal advanceCurr = Math.Min(remStage, payArray[i].paymentAmount);
                            payArray[i].paymentAmount -= advanceCurr;
                            remStage -= advanceCurr;

                            if (payArray[i].paymentDate < proStage.projectStage.CloseDate)
                            {
                                totalAdvanceCurr += advanceCurr;
                                totalAdvanceGel += advanceCurr * payArray[i].paymentRate;
                            }
                        }

                        proStage.projectStage.TotalGel = Math.Round(totalAdvanceGel + (proStage.projectStage.StageTotal - totalAdvanceCurr) * proStage.currencyRate, 2);
                    }
                    else
                        proStage.projectStage.TotalGel = 0;
                }
            }

            dw.ApplicationData.SaveChanges();

        }

        public void RefreshProjectAlerts()
        {

            var dw = this.Application.CreateDataWorkspace();

            foreach (Alert alrt in Alerts)
            {
                alrt.Project.AlertType = 0;
                alrt.Delete();
            }

            foreach (PaymentSchedule paySch in PaymentSchedules.Where(e => e.DueDate < DateTime.Today && e.TotalDue > (e.PaidTotal ?? 0)))
            {
                paySch.OverDue = paySch.TotalDue - (paySch.PaidTotal ?? 0);
                paySch.Project.AlertType = 1;

                Alert projectAlert = Alerts.AddNew();
                projectAlert.Project = paySch.Project;
                projectAlert.Message = "გადახდის ვადაგადაცილება " + (paySch.TotalDue - (paySch.PaidTotal ?? 0)).ToString() + " " + paySch.Project.Currency.Name;
            }

            foreach (ProjectStage proStage in ProjectStages.Where(e => e.EndDate < DateTime.Today && !e.Closed))
            {
                proStage.Project.AlertType = 2;
                Alert projectAlert = Alerts.AddNew();
                projectAlert.Project = proStage.Project;
                projectAlert.Message = "პროექტის ეტაპის \"" + proStage.Description + "\" ვადაგადაცილება " + (DateTime.Today - proStage.EndDate).Days + " დღე";

            }

            dw.ApplicationData.SaveChanges();

        }

        partial void Projects_Inserting(Project entity)
        {
            var lastEntry = Projects.OrderByDescending(mhOder => mhOder.Id).FirstOrDefault();

            string lastNumber="00/2001";
            if (lastEntry != null)
                lastNumber = lastEntry.ContractNumber;

            int middle = lastNumber.IndexOf('/');

            int lustIntNumber = Int32.Parse(lastNumber.Substring(0, middle));
            int lustYear  = Int32.Parse(lastNumber.Substring(middle+1, 4));

            string newNumber = "";

            if (DateTime.Today.Year > lustYear)
                newNumber = "01/" + DateTime.Today.Year.ToString();
            else
                newNumber = (lustIntNumber + 1).ToString() + "/" + lustYear;

            entity.ContractNumber = newNumber;

        }

        partial void PaymentSchedules_Inserting(PaymentSchedule entity)
        {
            entity.ProjectId = entity.Project.Id;
            entity.PaidTotal = 0;
            entity.OverDue = 0;
        }

        partial void ActualPayments_Deleted(ActualPayment entity)
        {
            updatePaymentSchedule(entity.ProjectId);
            updateProjectStages(entity.ProjectId);
        }

        partial void ActualPayments_Updated(ActualPayment entity)
        {
            updatePaymentSchedule(entity.ProjectId);
            updateProjectStages(entity.ProjectId);
        }

        partial void ActualPayments_Inserting(ActualPayment entity)
        {
            entity.ProjectId = entity.Project.Id;
        }

        partial void AlertRecalculations_Updated(AlertRecalculation entity)
        {
            RefreshProjectAlerts();
            getCurrencyRates();
 
        }

        partial void PaymentSchedules_Inserted(PaymentSchedule entity)
        {
            updatePaymentSchedule(entity.ProjectId);
        }

        partial void PaymentSchedules_Updated(PaymentSchedule entity)
        {
            if (entity.ClientChange)
                updatePaymentSchedule(entity.ProjectId);
        }

        partial void PaymentSchedules_Deleted(PaymentSchedule entity)
        {
            updatePaymentSchedule(entity.ProjectId);
        }

        partial void SaveChanges_CanExecute(ref bool result)
        {
            result = !Application.User.HasPermission(Permissions.ReadOnlyUser);
        }

        partial void GetRecalculationDate_CanExecute(ref bool result)
        {
            result = !Application.User.HasPermission(Permissions.ReadOnlyUser);
        }

        partial void PrintDocuments_Inserting(PrintDocument entity)
        {
            string wordDocument;

            DataWorkspace workspace = new DataWorkspace();
            var project = workspace.ApplicationData.Projects_SingleOrDefault(entity.ProjectId);

            if (project != null)
            {
                bool corporate = project.Client.ClientType == "Company";

                if (entity.DocumentName == "Contract")
                {

                    if (corporate)
                        wordDocument = HttpContext.Current.Server.MapPath(@"~\bin\ContractManager.Server\Templates\ContractTemplateCorporate.docx");
                    else
                        wordDocument = HttpContext.Current.Server.MapPath(@"~\bin\ContractManager.Server\Templates\ContractTemplate.docx");

                    Byte[] byteArray = File.ReadAllBytes(wordDocument);

                    using (MemoryStream mem = new MemoryStream())
                    {
                        mem.Write(byteArray, 0, (int)byteArray.Length);

                        using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(mem, true))
                        {
                            MainDocumentPart mainDocPart = wordDoc.MainDocumentPart;
                            InsertBookmarkValue(ref mainDocPart, "ContractNumber", project.ContractNumber, "18");
                            string clientName = project.Client.Name;
                            if (corporate)
                                clientName += " (ს/კ " + project.Client.TaxCode + ")";
                            else
                                clientName += " (პ/ნ " + project.Client.TaxCode + ")";
                            InsertBookmarkValue(ref mainDocPart, "ClientName", clientName, "18");
                            InsertCellValue(ref mainDocPart, "Footer_ClientName", clientName, "18");
                            InsertCellValue(ref mainDocPart, "Footer_ClientBillToAddress", project.Client.BillToAddress, "18");
                            InsertBookmarkValue(ref mainDocPart, "ClientAddress", project.Client.ShipToAddress, "18");
                            InsertCellValue(ref mainDocPart, "Footer_ClientShipToAddress", project.Client.ShipToAddress, "18");
                            InsertCellValue(ref mainDocPart, "Footer_ContactPerson", project.Client.ContactPerson, "18");
                            InsertCellValue(ref mainDocPart, "Footer_ContactPersonRole", project.Client.ContactPersonRole, "18");
                            InsertCellValue(ref mainDocPart, "Footer_Bank", project.Client.Bank, "18");
                            InsertCellValue(ref mainDocPart, "Footer_BankCode", project.Client.BankCode, "18");
                            InsertCellValue(ref mainDocPart, "Footer_BankAccount", project.Client.AccountNumber, "18");
                            InsertBookmarkValue(ref mainDocPart, "ContractDate", project.Created.Value.Date.ToString("dd-MMMM-yyyy წ.", new CultureInfo("ka-GE")), "18");
                            InsertBookmarkValue(ref mainDocPart, "ContractDescription", project.Description, "18");
                            InsertBookmarkValue(ref mainDocPart, "ClientDirector", project.Client.ContactPerson, "18");

                            string stageAmounts = "";
                            decimal totalProject = 0;

                            foreach (ProjectStage prStage in ProjectStages.Where(st => st.ProjectId == entity.ProjectId).OrderByDescending(st => st.EndDate))
                            {
                                if (!corporate)
                                {
                                    stageAmounts = GetProjectStageDisplayName(prStage.Description) + "ს თანხა - " + prStage.StageTotal + " " + prStage.Project.Currency.Name;
                                    InsertBookmarkValue(ref mainDocPart, "ProjectStages", stageAmounts, "18");
                                }
                                totalProject += prStage.StageTotal;
                            }

                            InsertBookmarkValue(ref mainDocPart, "ContractAmount", totalProject.ToString() + " " + project.Currency.Name, "18");
                            //InsertBookmarkValue(ref mainDocPart, "ProjectStages", stageAmounts);

                            mainDocPart.Document.Save();
                        }

                        entity.DocumentData = mem.ToArray();
                    }
                }
                else if (entity.DocumentName == "Acceptance")
                {
                    if (corporate)
                        wordDocument = HttpContext.Current.Server.MapPath(@"~\bin\ContractManager.Server\Templates\AcceptanceTemplateCorporate.docx");
                    else
                        wordDocument = HttpContext.Current.Server.MapPath(@"~\bin\ContractManager.Server\Templates\AcceptanceTemplate.docx");

                    Byte[] byteArray = File.ReadAllBytes(wordDocument);

                    using (MemoryStream mem = new MemoryStream())
                    {
                        mem.Write(byteArray, 0, (int)byteArray.Length);

                        using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(mem, true))
                        {
                            MainDocumentPart mainDocPart = wordDoc.MainDocumentPart;
                            string clientName = project.Client.Name;
                            if (corporate)
                                clientName += " (ს/კ " + project.Client.TaxCode + ")";
                            else
                                clientName += " (პ/ნ " + project.Client.TaxCode + ")";
                            InsertBookmarkValue(ref mainDocPart, "ClientName", clientName, "20");
                            InsertCellValue(ref mainDocPart, "Footer_ClientName", clientName, "20");
                            InsertBookmarkValue(ref mainDocPart, "ContractDate", project.Created.Value.Date.ToString("dd-MMMM-yyyy წ.", new CultureInfo("ka-GE")), "20");
                            InsertBookmarkValue(ref mainDocPart, "DocumentDate", DateTime.Today.ToString("dd-MMMM-yyyy წ.", new CultureInfo("ka-GE")), "20");
                            InsertBookmarkValue(ref mainDocPart, "ContractDescription", project.Description, "20");
                            InsertBookmarkValue(ref mainDocPart, "ClientAddress", project.Client.ShipToAddress, "20");
                            InsertBookmarkValue(ref mainDocPart, "ClientDirector", project.Client.ContactPerson, "20");

                            decimal totalProject = 0;

                            foreach (ProjectStage prStage in ProjectStages.Where(st => st.ProjectId == entity.ProjectId).OrderByDescending(st => st.EndDate))
                            {
                                totalProject += prStage.StageTotal;
                            }

                            InsertBookmarkValue(ref mainDocPart, "ContractAmount", totalProject.ToString() + " " + project.Currency.Name, "20");
                            //InsertBookmarkValue(ref mainDocPart, "ProjectStages", stageAmounts);

                            mainDocPart.Document.Save();
                        }

                        entity.DocumentData = mem.ToArray();

                    }
                }
            }
        }

        private string GetProjectStageDisplayName(string stageName)
        {
            if (stageName == "Stage1")
                return "I ეტაპი";
            if (stageName == "Stage2")
                return "II ეტაპი";
            if (stageName == "Stage3")
                return "III ეტაპი";
            if (stageName == "Stage4")
                return "IV ეტაპი";
            if (stageName == "Stage5")
                return "V ეტაპი";
            return stageName;
        }

        private void InsertBookmarkValue(ref MainDocumentPart mainPart, String bookmarkName, String bookmarkValue, string fontSizeVal)
        {

            Body body = mainPart.Document.GetFirstChild<Body>();
            var paras = body.Elements<Paragraph>();

            //Iterate through the paragraphs to find the bookmarks inside
            foreach (var para in paras)
            {
                var bookMarkStarts = para.Elements<BookmarkStart>();
                var bookMarkEnds = para.Elements<BookmarkEnd>();


                foreach (BookmarkStart bookMarkStart in bookMarkStarts)
                {
                    if (bookMarkStart.Name == bookmarkName)
                    {
                        //Get the id of the bookmark start to find the bookmark end
                        var id = bookMarkStart.Id.Value;
                        var bookmarkEnd = bookMarkEnds.Where(i => i.Id.Value == id).First();

                        DocumentFormat.OpenXml.Wordprocessing.Text docText = new DocumentFormat.OpenXml.Wordprocessing.Text(bookmarkValue);
                        DocumentFormat.OpenXml.Wordprocessing.Run docRun = new DocumentFormat.OpenXml.Wordprocessing.Run();
                        DocumentFormat.OpenXml.Wordprocessing.RunProperties runPro = new DocumentFormat.OpenXml.Wordprocessing.RunProperties();
                        RunFonts runFont = new RunFonts() { Ascii = "Sylfaen", HighAnsi = "Sylfaen" };
                        runPro.Append(runFont);
                        DocumentFormat.OpenXml.Wordprocessing.FontSize fontSize = new DocumentFormat.OpenXml.Wordprocessing.FontSize();
                        fontSize.Val = fontSizeVal;
                        runPro.Append(fontSize);

                        docRun.Append(runPro);
                        docRun.Append(docText);

                        if (bookmarkName == "ProjectStages")
                        {
                            var newPara = new Paragraph(docRun);
                            body.InsertAfter(newPara, para);
                        }
                        else
                            para.InsertAfter(docRun, bookmarkEnd);

                    }
                }
            }
        }

        private void InsertCellValue(ref MainDocumentPart mainPart, string bookmarkName, string bookmarkValue, string fontSizeVal)
        {
            Body body = mainPart.Document.GetFirstChild<Body>();
            DocumentFormat.OpenXml.Wordprocessing.Table table = body.Elements<DocumentFormat.OpenXml.Wordprocessing.Table>().First();

            // Find the second row in the table.
            TableRow row = table.Elements<TableRow>().ElementAt(0);

            // Find the third cell in the row.
            TableCell cell = row.Elements<TableCell>().ElementAt(0);

            var paras = cell.Elements<Paragraph>();

            foreach (var para in paras)
            {
                var bookMarkStarts = para.Elements<BookmarkStart>();
                var bookMarkEnds = para.Elements<BookmarkEnd>();


                foreach (BookmarkStart bookMarkStart in bookMarkStarts)
                {
                    if (bookMarkStart.Name == bookmarkName)
                    {
                        //Get the id of the bookmark start to find the bookmark end
                        var id = bookMarkStart.Id.Value;
                        var bookmarkEnd = bookMarkEnds.Where(i => i.Id.Value == id).First();

                        DocumentFormat.OpenXml.Wordprocessing.Text docText = new DocumentFormat.OpenXml.Wordprocessing.Text(bookmarkValue);
                        DocumentFormat.OpenXml.Wordprocessing.Run docRun = new DocumentFormat.OpenXml.Wordprocessing.Run();
                        DocumentFormat.OpenXml.Wordprocessing.RunProperties runPro = new DocumentFormat.OpenXml.Wordprocessing.RunProperties();
                        RunFonts runFont = new RunFonts() { Ascii = "Sylfaen", HighAnsi = "Sylfaen" };
                        runPro.Append(runFont);

                        DocumentFormat.OpenXml.Wordprocessing.FontSize fontSize = new DocumentFormat.OpenXml.Wordprocessing.FontSize();
                        fontSize.Val = fontSizeVal;
                        runPro.Append(fontSize);

                        docRun.Append(runPro);
                        docRun.Append(docText);
                        para.Append(docRun);

                        //var newPara = new Paragraph(docRun);
                        //cell.InsertAfter(newPara, para);

                    }
                }
            }


            //// Find the first paragraph in the table cell.
            //Paragraph p = cell.Elements<Paragraph>().First();

            //DocumentFormat.OpenXml.Wordprocessing.Text docText = new DocumentFormat.OpenXml.Wordprocessing.Text(cellValue);
            //DocumentFormat.OpenXml.Wordprocessing.Run docRun = new DocumentFormat.OpenXml.Wordprocessing.Run();
            //DocumentFormat.OpenXml.Wordprocessing.RunProperties runPro = new DocumentFormat.OpenXml.Wordprocessing.RunProperties();
            //RunFonts runFont = new RunFonts() { Ascii = "Sylfaen", HighAnsi = "Sylfaen" };
            //runPro.Append(runFont);
            //DocumentFormat.OpenXml.Wordprocessing.FontSize fontSize = new DocumentFormat.OpenXml.Wordprocessing.FontSize();
            //fontSize.Val = "18";
            //runPro.Append(fontSize);

            //docRun.Append(runPro);
            //docRun.Append(docText);
            //var newPara = new Paragraph(docRun);
            ////p.Append(newPara);
            //cell.InsertAfter(newPara, p);



        }

        private Paragraph GetParagraph(String text)
        {
            return new Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text(text)));
        }

        private void getCurrencyRates()
        {
            Common comm = Commons_SingleOrDefault(1);
            if (comm != null && comm.CurrencyDate == DateTime.Today)
                return;

            DateTime curDate = DateTime.Today;
            if (comm == null) curDate = DateTime.Today.AddMonths(-1); else curDate = comm.CurrencyDate;

            while (curDate < DateTime.Today)
            {
                curDate = curDate.AddDays(1);
                string url = "http://www.nbg.ge/rss.php?date=" + curDate.ToString("yyyy-MM-dd");
                XmlReader reader = XmlReader.Create(url);
                SyndicationFeed feed = SyndicationFeed.Load(reader);
                reader.Close();

                var dw = this.Application.CreateDataWorkspace();

                foreach (SyndicationItem item in feed.Items)
                {
                    String subject = item.Title.Text;
                    //DateTime curDate = Convert.ToDateTime(subject.Substring(15));

                    //if (curDate != DateTime.Today)
                    //    continue;                    

                    String summary = item.Summary.Text;

                    var currencyList = from cr in Currencies
                                       select cr;

                    while (summary.Contains("<tr>"))
                    {
                        summary = summary.Substring(summary.IndexOf("<tr>") + 4);
                        summary = summary.Substring(summary.IndexOf("<td>") + 4);
                        string currencyCode = summary.Substring(0, 3);

                        summary = summary.Substring(summary.IndexOf("<td>") + 4);
                        decimal currencyCoeff = Convert.ToDecimal(summary.Substring(0, summary.IndexOf(' ')));

                        summary = summary.Substring(summary.IndexOf("<td>") + 4);
                        decimal currencyRate = Convert.ToDecimal(summary.Substring(0, 6));

                        foreach (Currency cur in currencyList.Where(e => e.Name == currencyCode))
                        {
                            cur.CurrentRate = currencyRate;

                            CurrencyRate curRateEntry = dw.ApplicationData.CurrencyRates.AddNew();
                            curRateEntry.Currency = dw.ApplicationData.Currencies_SingleOrDefault(cur.Id);
                            curRateEntry.Rate = currencyRate;
                            curRateEntry.RateDate = curDate;

                        }

                    }

                }

                if (comm == null)
                    comm = dw.ApplicationData.Commons.AddNew();

                comm.CurrencyDate = curDate;

                dw.ApplicationData.SaveChanges();

            }

        }

        public static void Log(string logMessage, TextWriter w)
        {
            w.Write("\r\nLog Entry : ");
            w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                DateTime.Now.ToLongDateString());
            w.WriteLine("  :");
            w.WriteLine("  :{0}", logMessage);
            w.WriteLine("-------------------------------");
        }

        partial void AlertRecalculations_Inserted(AlertRecalculation entity)
        {
            RefreshProjectAlerts();
            getCurrencyRates();
        }

        partial void ProjectStages_Updated(ProjectStage entity)
        {
            if (entity.ClientChange)
                updateProjectStages(entity.ProjectId);
        }

        partial void ProjectStages_Inserted(ProjectStage entity)
        {
            updateProjectStages(entity.ProjectId);
        }

        partial void ProjectStages_Deleted(ProjectStage entity)
        {
            updateProjectStages(entity.ProjectId);
        }

        partial void ProjectStages_Inserting(ProjectStage entity)
        {
            entity.ProjectId = entity.Project.Id;
        }

        partial void ProjectReport_PreprocessQuery(ref IQueryable<Project> query)
        {

        }

        partial void DebtReportCommands_Inserted(DebtReportCommand entity)
        {
            var dw = this.Application.CreateDataWorkspace();

            foreach (DebtReportData debtRepRow in DebtReportDatas)
            {
                debtRepRow.Delete();
            }

            var reportQuery = from Project pr in Projects
                              let ProjectTotal = pr.ProjectStages.Sum(ps => ps.StageTotal)
                              let TotalAccrual = pr.ProjectStages.Where(pr => pr.Closed).Sum(pr => pr.StageTotal)
                              let TotalAccrualGel = pr.ProjectStages.Where(pr => pr.Closed).Sum(pr => pr.TotalGel)
                              let TotalPaid = pr.ActualPayments.Sum(ap => ap.Paid)
                              let TotalPaidGel = pr.ActualPayments.Sum(ap => ap.PaidGel)
                              select new { Project = pr, ProjectTotal, TotalAccrual, TotalAccrualGel, TotalPaid, TotalPaidGel };

            foreach (var queryRow in reportQuery)
            {
                DebtReportData reportRow = dw.ApplicationData.DebtReportDatas.AddNew();
                reportRow.Project = dw.ApplicationData.Projects_SingleOrDefault(queryRow.Project.Id);
                reportRow.TotalProject = queryRow.ProjectTotal;
                reportRow.TotalAccrual = queryRow.TotalAccrual;
                reportRow.TotalAccrualGel = queryRow.TotalAccrualGel;
                reportRow.TotalPaid = queryRow.TotalPaid;
                reportRow.TotalPaidGel = queryRow.TotalPaidGel;
                reportRow.ProjectBalance = reportRow.TotalProject - reportRow.TotalPaid;
                reportRow.Debt = reportRow.TotalAccrual - reportRow.TotalPaid;
                reportRow.DebtGel = reportRow.TotalAccrualGel - reportRow.TotalPaidGel;
            }




            dw.ApplicationData.SaveChanges();
        }

        partial void ProfitReportCommands_Inserted(ProfitReportCommand entity)
        {
            var dw = this.Application.CreateDataWorkspace();

            foreach (ProfitReportData proRepRow in ProfitReportDatas)
            {
                proRepRow.Delete();
            }

            DateTime startDate = entity.StartDate;
            DateTime endDate = entity.EndDate;

            var reportQuery = from Project pr in Projects
                              from ProjectStage ps in pr.ProjectStages.Where(ps=>ps.Project.Id == pr.Id)
                              let TotalAccrual = pr.ProjectStages.Where(ps => ps.Closed && ps.CloseDate <= endDate && ps.CloseDate >= startDate).Sum(ps=>ps.StageTotal)
                              let TotalSalary = ps.Salaries.Where(ps => ps.PaymentDate <= endDate && ps.PaymentDate >= startDate).Sum(ps => ps.Total)
                              let TotalOtherExp = ps.Materials.Where(ps => ps.ExpenseDate <= endDate && ps.ExpenseDate >= startDate).Sum(ps => ps.Total)
                              select new { Project = pr, TotalAccrual, TotalSalary, TotalOtherExp };


            //reportQuery.GroupBy()

            foreach (var queryRow in reportQuery.GroupBy(p=>p.Project))
            {
                ProfitReportData reportRow = dw.ApplicationData.ProfitReportDatas.AddNew();
                reportRow.Project = dw.ApplicationData.Projects_SingleOrDefault(queryRow.Key.Id);
                reportRow.Revenue = queryRow.Max(s=>s.TotalAccrual);
                reportRow.Salary = queryRow.Sum(s => s.TotalSalary);
                reportRow.Other = queryRow.Sum(s => s.TotalOtherExp);
                reportRow.Profit = reportRow.Revenue - (reportRow.Salary + reportRow.Other);
            }

            dw.ApplicationData.SaveChanges();

        }

        partial void ExcelExports_Inserting(ExcelExport entity)
        {
            if (entity.TableName == "DebtReport")
            {
                string excelDocument;

                excelDocument = HttpContext.Current.Server.MapPath(@"~\bin\ContractManager.Server\Templates\ExcelTemplate.xlsx");

                Byte[] byteArray = File.ReadAllBytes(excelDocument);

                using (MemoryStream mem = new MemoryStream())
                {
                    mem.Write(byteArray, 0, (int)byteArray.Length);

                    using (SpreadsheetDocument spreadSheet = SpreadsheetDocument.Open(mem, true))
                    {
                        // Get the SharedStringTablePart. If it does not exist, create a new one.
                        SharedStringTablePart shareStringPart;
                        if (spreadSheet.WorkbookPart.GetPartsOfType<SharedStringTablePart>().Count() > 0)
                        {
                            shareStringPart = spreadSheet.WorkbookPart.GetPartsOfType<SharedStringTablePart>().First();
                        }
                        else
                        {
                            shareStringPart = spreadSheet.WorkbookPart.AddNewPart<SharedStringTablePart>();
                        }


                        // Insert a new worksheet.
                        //WorksheetPart worksheetPart = InsertWorksheet(spreadSheet.WorkbookPart);
                        WorksheetPart worksheetPart = spreadSheet.WorkbookPart.WorksheetParts.First();

                        // Insert the text into the SharedStringTablePart.
                        int index = InsertSharedStringItem("პროექტი", shareStringPart);

                        // Insert cell A1 into the new worksheet.
                        Cell cell = InsertCellInWorksheet("A", 1, worksheetPart);

                        // Set the value of cell A1.
                        cell.CellValue = new CellValue(index.ToString());
                        cell.DataType = new EnumValue<CellValues>(CellValues.SharedString);
                        ///////////////////////////////////////////////////////////////////////

                        // Insert the text into the SharedStringTablePart.
                        index = InsertSharedStringItem("ვალუტა", shareStringPart);

                        // Insert cell A1 into the new worksheet.
                        cell = InsertCellInWorksheet("B", 1, worksheetPart);

                        // Set the value of cell A1.
                        cell.CellValue = new CellValue(index.ToString());
                        cell.DataType = new EnumValue<CellValues>(CellValues.SharedString);
                        ///////////////////////////////////////////////////////////////////////

                        // Insert the text into the SharedStringTablePart.
                        index = InsertSharedStringItem("პროექტის თანხა", shareStringPart);

                        // Insert cell A1 into the new worksheet.
                        cell = InsertCellInWorksheet("C", 1, worksheetPart);

                        // Set the value of cell A1.
                        cell.CellValue = new CellValue(index.ToString());
                        cell.DataType = new EnumValue<CellValues>(CellValues.SharedString);
                        ///////////////////////////////////////////////////////////////////////

                        // Insert the text into the SharedStringTablePart.
                        index = InsertSharedStringItem("შესრულებული", shareStringPart);

                        // Insert cell A1 into the new worksheet.
                        cell = InsertCellInWorksheet("D", 1, worksheetPart);

                        // Set the value of cell A1.
                        cell.CellValue = new CellValue(index.ToString());
                        cell.DataType = new EnumValue<CellValues>(CellValues.SharedString);
                        ///////////////////////////////////////////////////////////////////////

                        // Insert the text into the SharedStringTablePart.
                        index = InsertSharedStringItem("გადახდილი", shareStringPart);

                        // Insert cell A1 into the new worksheet.
                        cell = InsertCellInWorksheet("E", 1, worksheetPart);

                        // Set the value of cell A1.
                        cell.CellValue = new CellValue(index.ToString());
                        cell.DataType = new EnumValue<CellValues>(CellValues.SharedString);
                        ///////////////////////////////////////////////////////////////////////

                        // Insert the text into the SharedStringTablePart.
                        index = InsertSharedStringItem("პროექტის ნაშთი", shareStringPart);

                        // Insert cell A1 into the new worksheet.
                        cell = InsertCellInWorksheet("F", 1, worksheetPart);

                        // Set the value of cell A1.
                        cell.CellValue = new CellValue(index.ToString());
                        cell.DataType = new EnumValue<CellValues>(CellValues.SharedString);
                        ///////////////////////////////////////////////////////////////////////

                        // Insert the text into the SharedStringTablePart.
                        index = InsertSharedStringItem("დავალიანება", shareStringPart);

                        // Insert cell A1 into the new worksheet.
                        cell = InsertCellInWorksheet("G", 1, worksheetPart);

                        // Set the value of cell A1.
                        cell.CellValue = new CellValue(index.ToString());
                        cell.DataType = new EnumValue<CellValues>(CellValues.SharedString);
                        ///////////////////////////////////////////////////////////////////////

                        // Insert the text into the SharedStringTablePart.
                        index = InsertSharedStringItem("დარიცხული ლარი", shareStringPart);

                        // Insert cell A1 into the new worksheet.
                        cell = InsertCellInWorksheet("H", 1, worksheetPart);

                        // Set the value of cell A1.
                        cell.CellValue = new CellValue(index.ToString());
                        cell.DataType = new EnumValue<CellValues>(CellValues.SharedString);
                        ///////////////////////////////////////////////////////////////////////

                        // Insert the text into the SharedStringTablePart.
                        index = InsertSharedStringItem("გადახდილი ლარი", shareStringPart);

                        // Insert cell A1 into the new worksheet.
                        cell = InsertCellInWorksheet("I", 1, worksheetPart);

                        // Set the value of cell A1.
                        cell.CellValue = new CellValue(index.ToString());
                        cell.DataType = new EnumValue<CellValues>(CellValues.SharedString);
                        ///////////////////////////////////////////////////////////////////////

                        // Insert the text into the SharedStringTablePart.
                        index = InsertSharedStringItem("დავალიანება ლარი", shareStringPart);

                        // Insert cell A1 into the new worksheet.
                        cell = InsertCellInWorksheet("J", 1, worksheetPart);

                        // Set the value of cell A1.
                        cell.CellValue = new CellValue(index.ToString());
                        cell.DataType = new EnumValue<CellValues>(CellValues.SharedString);
                        ///////////////////////////////////////////////////////////////////////

                        uint i = 1;

                        foreach (DebtReportData debtReportRow in DebtReportDatas)
                        {
                            i++;

                            // Insert the text into the SharedStringTablePart.
                            index = InsertSharedStringItem(debtReportRow.Project.ContractNumber, shareStringPart);

                            // Insert cell A1 into the new worksheet.
                            cell = InsertCellInWorksheet("A", i, worksheetPart);

                            // Set the value of cell A1.
                            cell.CellValue = new CellValue(index.ToString());
                            cell.DataType = new EnumValue<CellValues>(CellValues.SharedString);
                            ///////////////////////////////////////////////////////////////////////

                            // Insert the text into the SharedStringTablePart.
                            index = InsertSharedStringItem(debtReportRow.Project.Currency.Name, shareStringPart);

                            // Insert cell A1 into the new worksheet.
                            cell = InsertCellInWorksheet("B", i, worksheetPart);

                            // Set the value of cell A1.
                            cell.CellValue = new CellValue(index.ToString());
                            cell.DataType = new EnumValue<CellValues>(CellValues.SharedString);
                            ///////////////////////////////////////////////////////////////////////

                            // Insert cell A1 into the new worksheet.
                            cell = InsertCellInWorksheet("C", i, worksheetPart);

                            // Set the value of cell A1.
                            cell.CellValue = new CellValue(debtReportRow.TotalProject.ToString());
                            cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                            ///////////////////////////////////////////////////////////////////////

                            // Insert cell A1 into the new worksheet.
                            cell = InsertCellInWorksheet("D", i, worksheetPart);

                            // Set the value of cell A1.
                            cell.CellValue = new CellValue(debtReportRow.TotalAccrual.ToString());
                            cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                            ///////////////////////////////////////////////////////////////////////

                            // Insert cell A1 into the new worksheet.
                            cell = InsertCellInWorksheet("E", i, worksheetPart);

                            // Set the value of cell A1.
                            cell.CellValue = new CellValue(debtReportRow.TotalPaid.ToString());
                            cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                            ///////////////////////////////////////////////////////////////////////

                            // Insert cell A1 into the new worksheet.
                            cell = InsertCellInWorksheet("F", i, worksheetPart);

                            // Set the value of cell A1.
                            cell.CellValue = new CellValue(debtReportRow.ProjectBalance.ToString());
                            cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                            ///////////////////////////////////////////////////////////////////////

                            // Insert cell A1 into the new worksheet.
                            cell = InsertCellInWorksheet("G", i, worksheetPart);

                            // Set the value of cell A1.
                            cell.CellValue = new CellValue(debtReportRow.Debt.ToString());
                            cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                            ///////////////////////////////////////////////////////////////////////

                            // Insert cell A1 into the new worksheet.
                            cell = InsertCellInWorksheet("H", i, worksheetPart);

                            // Set the value of cell A1.
                            cell.CellValue = new CellValue(debtReportRow.TotalAccrualGel.ToString());
                            cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                            ///////////////////////////////////////////////////////////////////////

                            // Insert cell A1 into the new worksheet.
                            cell = InsertCellInWorksheet("I", i, worksheetPart);

                            // Set the value of cell A1.
                            cell.CellValue = new CellValue(debtReportRow.TotalPaidGel.ToString());
                            cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                            ///////////////////////////////////////////////////////////////////////

                            // Insert cell A1 into the new worksheet.
                            cell = InsertCellInWorksheet("J", i, worksheetPart);

                            // Set the value of cell A1.
                            cell.CellValue = new CellValue(debtReportRow.DebtGel.ToString().ToString());
                            cell.DataType = new EnumValue<CellValues>(CellValues.Number);
                            ///////////////////////////////////////////////////////////////////////

                        }

                        // Save the new worksheet.
                        worksheetPart.Worksheet.Save();
                    }

                    entity.ExcelData = mem.ToArray();
                }

            }

        }

        private static int InsertSharedStringItem(string text, SharedStringTablePart shareStringPart)
        {
            // If the part does not contain a SharedStringTable, create one.
            if (shareStringPart.SharedStringTable == null)
            {
                shareStringPart.SharedStringTable = new SharedStringTable();
            }

            int i = 0;

            // Iterate through all the items in the SharedStringTable. If the text already exists, return its index.
            foreach (SharedStringItem item in shareStringPart.SharedStringTable.Elements<SharedStringItem>())
            {
                if (item.InnerText == text)
                {
                    return i;
                }

                i++;
            }

            // The text does not exist in the part. Create the SharedStringItem and return its index.
            shareStringPart.SharedStringTable.AppendChild(new SharedStringItem(new DocumentFormat.OpenXml.Spreadsheet.Text(text)));
            shareStringPart.SharedStringTable.Save();

            return i;
        }

        // Given a WorkbookPart, inserts a new worksheet.
        private static WorksheetPart InsertWorksheet(WorkbookPart workbookPart)
        {
            // Add a new worksheet part to the workbook.
            WorksheetPart newWorksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            newWorksheetPart.Worksheet = new Worksheet(new SheetData());
            newWorksheetPart.Worksheet.Save();

            Sheets sheets = workbookPart.Workbook.GetFirstChild<Sheets>();
            string relationshipId = workbookPart.GetIdOfPart(newWorksheetPart);

            // Get a unique ID for the new sheet.
            uint sheetId = 1;
            if (sheets.Elements<Sheet>().Count() > 0)
            {
                sheetId = sheets.Elements<Sheet>().Select(s => s.SheetId.Value).Max() + 1;
            }

            string sheetName = "Sheet" + sheetId;

            // Append the new worksheet and associate it with the workbook.
            Sheet sheet = new Sheet() { Id = relationshipId, SheetId = sheetId, Name = sheetName };
            sheets.Append(sheet);
            workbookPart.Workbook.Save();

            return newWorksheetPart;
        }

        // Given a column name, a row index, and a WorksheetPart, inserts a cell into the worksheet. 
        // If the cell already exists, returns it. 
        private static Cell InsertCellInWorksheet(string columnName, uint rowIndex, WorksheetPart worksheetPart)
        {
            Worksheet worksheet = worksheetPart.Worksheet;
            SheetData sheetData = worksheet.GetFirstChild<SheetData>();
            string cellReference = columnName + rowIndex;

            // If the worksheet does not contain a row with the specified row index, insert one.
            Row row;
            if (sheetData.Elements<Row>().Where(r => r.RowIndex == rowIndex).Count() != 0)
            {
                row = sheetData.Elements<Row>().Where(r => r.RowIndex == rowIndex).First();
            }
            else
            {
                row = new Row() { RowIndex = rowIndex };
                sheetData.Append(row);
            }

            // If there is not a cell with the specified column name, insert one.  
            if (row.Elements<Cell>().Where(c => c.CellReference.Value == columnName + rowIndex).Count() > 0)
            {
                return row.Elements<Cell>().Where(c => c.CellReference.Value == cellReference).First();
            }
            else
            {
                // Cells must be in sequential order according to CellReference. Determine where to insert the new cell.
                Cell refCell = null;
                foreach (Cell cell in row.Elements<Cell>())
                {
                    if (string.Compare(cell.CellReference.Value, cellReference, true) > 0)
                    {
                        refCell = cell;
                        break;
                    }
                }

                Cell newCell = new Cell() { CellReference = cellReference };
                row.InsertBefore(newCell, refCell);

                worksheet.Save();
                return newCell;
            }
        }

    }
}