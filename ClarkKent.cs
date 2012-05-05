using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

using FogCreek.FogBugz;
using FogCreek.FogBugz.Plugins;
using FogCreek.FogBugz.Plugins.Api;
using FogCreek.FogBugz.Plugins.Interfaces;
using FogCreek.FogBugz.UI;

using FogCreek.FogBugz.UI.EditableTable;
using FogCreek.FogBugz.Plugins.Entity;
using FogCreek.FogBugz.Database.Entity;

namespace ClarkKent
{
    public class ClarkKent : Plugin, IPluginBinaryPageDisplay, IPluginPageDisplay, IPluginExtrasMenu
    {
        public ClarkKent(CPluginApi api) : base(api)
        {
        }

        public CNavMenuLink[] ExtrasMenuLinks()
        {
            if (api.Person.GetCurrentPerson().GetPermissionLevel() > PermissionLevel.Community)
            {
                return new CNavMenuLink[] { new CNavMenuLink("Clark Kent", api.Url.PluginPageUrl()) };
            }
            else
            {
                return new CNavMenuLink[] { };
            }
        }

        #region IPluginPageDisplay Members
        public PermissionLevel PageVisibility()
        {
            return PermissionLevel.Normal;
        }

        public string PageDisplay()
        {
            String action = api.Request[api.AddPluginPrefix("sAction")] == null ? 
                null : api.Request[api.AddPluginPrefix("sAction")].ToString();

            FormParms parms = new FormParms();

            if (action != null &&
                api.Request[api.AddPluginPrefix("actionToken")] != null &&
                api.Security.ValidateActionToken(api.Request[api.AddPluginPrefix("actionToken")], "fetch"))
            {

                if (action == "fetch")
                {
                    parms.loadFormParms(api);
                }
                else if (action == "next")
                {
                    parms.loadFormParms(api);
                    parms.nextMonth();
                }
                else if (action == "prev")
                {
                    parms.loadFormParms(api);
                    parms.prevMonth();
                }
            }

            string html = InputForm(parms);

            CTimeInterval[] intervals = getIntervals(parms);

            if (intervals != null && intervals.Length > 0)
            {
                html += DownloadLink(parms) + "<p/>";
                html += HtmlTableData(intervals) + "<p/>";
                html += DownloadLink(parms);
            }
            else
            {
                html += "No time intervals found.";
            }

            html += "<p/>" + footer() + "<p/>";

            return html;
        }
        #endregion

        #region IPluginBinaryPageDisplay Members

        public byte[] BinaryPageDisplay()
        {
            string csvData = "Start,End,Duration (Min),Project,Case,Title,User" + Environment.NewLine;
            byte[] rawData = null;

            try
            {
                FormParms parms = new FormParms();

                if (api.Request[api.AddPluginPrefix("actionToken")] != null &&
                    api.Security.ValidateActionToken(api.Request[api.AddPluginPrefix("actionToken")], "download"))
                {
                    parms.loadFormParms(api);

                    CTimeInterval[] intervals = getIntervals(parms);

                    foreach (CTimeInterval interval in intervals)
                    {
                        string intervalStart = api.TimeZone.DateTimeString(api.TimeZone.CTZFromUTC(interval.dtStart), false);
                        string intervalEnd = api.TimeZone.DateTimeString(api.TimeZone.CTZFromUTC(interval.dtEnd), false);

                        CBug bug = api.Bug.GetBug(interval.ixBug);
                        string bugNumber = (bug == null ? "????" : bug.ixBug.ToString());
                        string title = "\"" + (bug == null ? "Missing bug info" : bug.sTitle) + "\"";

                        CProject project = api.Project.GetProject(bug.ixProject);
                        string projectName = "\"" + (project == null ? "Missing project info" : project.sProject) + "\"";

                        CPerson person = api.Person.GetPerson(interval.ixPerson);
                        string personName = "\"" + (person == null ? "Missing user innfo" : person.sFullName) + "\"";

                        TimeSpan timespan = interval.dtEnd.Subtract(interval.dtStart);
                        string duration = GetMinutes(timespan.TotalSeconds).ToString();

                        csvData += intervalStart + "," + intervalEnd + "," + duration + "," +
                            projectName + "," + bugNumber + "," + title + "," + personName + Environment.NewLine;
                    }

                    rawData = StringToByteArray(csvData, EncodingType.ASCII);

                    api.Response.ContentType = "text/plain";
                    api.Response.AddHeader("Content-Disposition", "attachment; filename=timeintervals.csv");

                    return rawData;
                }
            }
            catch (Exception ex)
            {
                Console.Write("An error occured: " + ex.Message);
            }
            return rawData;
        }

        public PermissionLevel BinaryPageVisibility()
        {
            return PermissionLevel.Normal;
        }

        #endregion

        protected string HtmlTableData(CTimeInterval[] intervals)
        {
            double totalMinutes = 0;

            CEditableTable table = new CEditableTable("intervals");
            
            table.Header.AddCell("Start");
            table.Header.AddCell("End");
            table.Header.AddCell("Duration (Hrs)");
            table.Header.AddCell("Duration (Min)");
            table.Header.AddCell("Project");
            table.Header.AddCell("Case");
            table.Header.AddCell("Title");
            table.Header.AddCell("User");

            int colCount = table.Header.Cells.Count;

            foreach (CTimeInterval interval in intervals)
            {
                string intervalStart = api.TimeZone.DateTimeString(api.TimeZone.CTZFromUTC(interval.dtStart), false);
                string intervalEnd = api.TimeZone.DateTimeString(api.TimeZone.CTZFromUTC(interval.dtEnd), false);

                CBug bug = api.Bug.GetBug(interval.ixBug);

                string bugNumber = (bug == null ? "????" : bug.ixBug.ToString());
                string title = HttpUtility.HtmlEncode(bug == null ? "Missing bug info" : bug.sTitle);

                CProject project = api.Project.GetProject(bug.ixProject);
                string projectName = HttpUtility.HtmlEncode(project == null ? "Missing project info" : project.sProject);

                CPerson person = api.Person.GetPerson(interval.ixPerson);
                string personName = HttpUtility.HtmlEncode(person == null ? "Missing user info" : person.sFullName);

                TimeSpan timespan = interval.dtEnd.Subtract(interval.dtStart);
                double intervalMinutes = GetMinutes(timespan.TotalSeconds);
                totalMinutes += intervalMinutes;

                CEditableTableRow row = new CEditableTableRow();
                row.sRowId = interval.ixInterval.ToString();
                row.AddCell(intervalStart);
                row.AddCell(intervalEnd);
                row.AddCell(GetFormatedHoursAndMinutes(intervalMinutes));
                row.AddCell(intervalMinutes.ToString());
                row.AddCell(projectName);
                row.AddCell(BugDisplay.Link(bug, bugNumber));
                row.AddCell(title);
                row.AddCell(personName);

                table.Body.AddRow(row);
            }

            table.Footer.AddCell("");
            table.Footer.AddCell("Total");
            table.Footer.AddCell(GetFormatedHoursAndMinutes(totalMinutes));
            table.Footer.AddCell(totalMinutes.ToString());
            table.Footer.AddCell("");
            table.Footer.AddCell("");
            table.Footer.AddCell("");
            table.Footer.AddCell("");

            return table.RenderHtml();
        }

        protected string InputForm(FormParms parms)
        {
            string startHtml = Forms.DateInputCTZ("start", api.AddPluginPrefix("start"), parms.getStart());
            string endHtml = Forms.DateInputCTZ("end", api.AddPluginPrefix("end"), parms.getEnd());

            CPerson[] persons = getPersons();
            string[] personOptions = new string[persons.Length + 1];
            string[] personValues = new string[persons.Length + 1];
            
            personOptions[0] = "All";
            personValues[0] = "0";
            int index = 1;

            foreach(CPerson person in persons) {
                personOptions[index] = person.sFullName.Trim();
                personValues[index] = person.ixPerson.ToString();
                index++;
            }

            string indexValue = "0";

            if (parms.getPerson() != null)
            {
                indexValue = parms.getPerson().ixPerson.ToString();
            }

            string personsHtml = Forms.SelectInput(api.AddPluginPrefix("person"), personOptions, indexValue, personValues);

            CProject[] projects = getProjects();
            string[] projectOptions = new string[projects.Length + 1];
            string[] projectValues = new string[projects.Length + 1];

            projectOptions[0] = "All";
            projectValues[0] = "0";
            index = 1;

            foreach (CProject project in projects)
            {
                projectOptions[index] = project.sProject;
                projectValues[index] = project.ixProject.ToString();
                index++;
            }

            indexValue = "0";
            if  (parms.getProject() != null) 
            {
                indexValue = parms.getProject().ixProject.ToString();
            }

            string projectsHtml = Forms.SelectInput(api.AddPluginPrefix("project"), projectOptions, indexValue, projectValues);            

            string html = string.Format(@"
            <form action=""{0}"" method=""post"">
            <input type=""hidden"" name=""{1}actionToken"" value=""{2}"" />
            <input type=""hidden"" name=""{1}sAction"" value=""fetch"" />
            <style>
                
            </style>
            <table cellspacing=""0"" border=""0"" width=""400"">
            <tr>
                <td nowrap>Start</td><td>{3}</td>
                <td nowrap>End</td><td>{4}</td>
                <td nowrap><input type=""submit"" value=""Prev Month"" onclick=""{1}sAction.value='prev';return true;""/></td>
                <td nowrap><input type=""submit"" value=""Next Month"" onclick=""{1}sAction.value='next';return true;""/></td>
            </tr>
            <tr>
                <td nowrap>User</td><td>{5}</td>
                <td nowrap>Project</td><td>{6}</td>
                <td nowrap><input type=""submit"" value=""Fetch"" onclick=""{1}sAction.value='fetch';return true;""/></td>
                <td>&nbsp;</td>
            </tr>
            </table>
            </form>", 
            api.Url.PluginPageUrl(), api.PluginPrefix, api.Security.GetActionToken("fetch"), 
            startHtml, endHtml, personsHtml, projectsHtml);

            return html;
        }

        protected string footer()
        {
            return "Please report bugs and requests to <a href=\"mailto:support@truecool.com\">TrueCool.com Inc.</a>";
        }

        protected string donate()
        {
            return "If you find this plugin useful please donate ($10.00) to support further development.<p/>" +
                    "<form action=\"https://www.paypal.com/cgi-bin/webscr\" method=\"post\">" +
                    "<input type=\"hidden\" name=\"cmd\" value=\"_s-xclick\">" +
                    "<input type=\"hidden\" name=\"hosted_button_id\" value=\"6466097\">" +
                    "<input type=\"image\" src=\"https://www.paypal.com/en_US/i/btn/btn_donateCC_LG.gif\" border=\"0\" name=\"submit\" alt=\"PayPal - The safer, easier way to pay online!\">" +
                    "<img alt=\"\" border=\"0\" src=\"https://www.paypal.com/en_US/i/scr/pixel.gif\" width=\"1\" height=\"1\">" +
                    "</form>";
        }

        protected string DownloadLink(FormParms parms)
        {
            string html = String.Format(@"<a href=""{0}&{1}actionToken={2}&{1}start={3}&{1}end={4}",
                HttpUtility.HtmlEncode(api.Url.PluginBinaryPageUrl()), 
                api.PluginPrefix, 
                api.Security.GetActionToken("download"), 
                HttpUtility.HtmlEncode(parms.getStart().ToShortDateString()), 
                HttpUtility.HtmlEncode(parms.getEnd().ToShortDateString()));

            if (parms.getPerson() != null) {
                html += "&" + api.PluginPrefix + "person=" + parms.getPerson().ixPerson;
            }

            if (parms.getProject() != null) {
                html += "&" + api.PluginPrefix + "project=" + parms.getProject().ixProject;
            }

            html += "\"><b>CSV Download</b></a>";

            return html;
        }

        protected CTimeInterval[] getIntervals(FormParms parms)
        {
            CTimeIntervalQuery query = api.TimeInterval.NewTimeIntervalQuery();

            query.IgnorePermissions = true;

            query.AddWhere("dtStart >= @start");
            query.SetParamDate("start", api.TimeZone.UTCFromCTZ(parms.getStart()));
            query.AddWhere("dtEnd <= @end");
            query.SetParamDate("end", api.TimeZone.UTCFromCTZ(parms.getEnd()));

            if (parms.getPerson() != null && parms.getPerson().ixPerson != 0)
            {
                query.AddWhere("ixPerson = @person");
                query.SetParamInt("person", parms.getPerson().ixPerson);
            }

            if (parms.getProject() != null && parms.getProject().ixProject != 0) {
                query.AddLeftJoin("Bug", "TimeInterval.ixBug = Bug.ixBug");
                query.AddWhere("Bug.ixProject = @project");
                query.SetParamInt("project", parms.getProject().ixProject);
            }

            query.AddOrderBy("TimeInterval.dtStart");

            CTimeInterval[] intervals = query.List();
            return intervals; 
        }

        protected CPerson[] getPersons()
        {
            CPersonQuery query = api.Person.NewPersonQuery();
            query.IgnorePermissions = true;
            CPerson[] persons = query.List();
            return persons;
        }

        protected CProject[] getProjects()
        {
            CProjectQuery query = api.Project.NewProjectQuery();
            query.IgnorePermissions = true;
            CProject[] projects = query.List();
            return projects;
        }

        public double GetMinutes(double seconds) {
            double minutes = seconds / 60;
            return Math.Round(minutes, 2);
        }

        public String GetFormatedHoursAndMinutes(double minutes)
        {
            TimeSpan span = TimeSpan.FromMinutes(minutes);
            return string.Format("{0:0}:{1:D2}", (span.Days * 24) + span.Hours, span.Minutes);
        }

        #region EncodingType enum
        /// <summary> 
        /// Encoding Types. 
        /// </summary> 
        protected enum EncodingType
        {
            ASCII,
            Unicode,
            UTF7,
            UTF8
        }
        #endregion 

        #region StringToByteArray
        /// <summary> 
        /// Converts a string to a byte array using specified encoding. 
        /// </summary> 
        /// <param name="str">String to be converted.</param> 
        /// <param name="encodingType">EncodingType enum.</param> 
        /// <returns>byte array</returns> 
        protected byte[] StringToByteArray(string str, EncodingType encodingType)
        {
            System.Text.Encoding encoding = null;
            switch (encodingType)
            {
                case EncodingType.ASCII:
                    encoding = new System.Text.ASCIIEncoding();
                    break;
                case EncodingType.Unicode:
                    encoding = new System.Text.UnicodeEncoding();
                    break;
                case EncodingType.UTF7:
                    encoding = new System.Text.UTF7Encoding();
                    break;
                case EncodingType.UTF8:
                    encoding = new System.Text.UTF8Encoding();
                    break;
            }
            return encoding.GetBytes(str);
        }
        #endregion
    }
}
