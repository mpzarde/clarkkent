﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using FogCreek.FogBugz.Plugins;
using FogCreek.FogBugz.Plugins.Api;

using FogCreek.FogBugz.Plugins.Entity;
using FogCreek.FogBugz.Database.Entity;

namespace ClarkKent
{
    public class FormParms
    {
        private DateTime _start;
        private DateTime _end;
        private CProject _project;
        private CPerson _person;

        public FormParms()
        {
            _start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            _end = _start.AddMonths(1).AddTicks(-1);
            _project = null;
            _person = null;
        }

        public DateTime getStart()
        {
            return _start;
        }

        public DateTime getEnd()
        {
            return _end;
        }

        public CProject getProject()
        {
            return _project;
        }

        public CPerson getPerson()
        {
            return _person;
        }

        public void loadFormParms(CPluginApi api)
        {
            string startValue = api.Request[api.AddPluginPrefix("start")];

            if (startValue != null && startValue.Length > 0)
            {
                _start = DateTime.Parse(startValue);
            }

            string endValue = api.Request[api.AddPluginPrefix("end")];

            if (endValue != null && endValue.Length > 0)
            {
                _end = DateTime.Parse(endValue);
            }

            if (_start == null) {
                _start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }

            if (_end == null)
            {
                _end = _start.AddMonths(1).AddTicks(-1);
            }
            else
            {
                _end = _end.AddHours(24).AddTicks(-1);
            }

            string personKeyValue = api.Request[api.AddPluginPrefix("person")];

            if (personKeyValue != null)
            {
                int personKey = Int16.Parse(personKeyValue);
                _person = api.Person.GetPerson(personKey);
            }
            else
            {
                _person = null;
            }

            string projectKeyValue = api.Request[api.AddPluginPrefix("project")];

            if (projectKeyValue != null)
            {
                int projectKey = Int16.Parse(projectKeyValue);
                _project = api.Project.GetProject(projectKey);
            }
            else
            {
                _project = null;
            }
            
        }
    }
}