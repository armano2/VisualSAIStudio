﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VisualSAIStudio.SmartScripts;
using WeifenLuo.WinFormsUI.Docking;

namespace VisualSAIStudio
{
    public partial class ScratchWindow : DockContent
    {

        SmartEventsCollection events = new SmartEventsCollection();

        public EventHandler ElementSelected = delegate { };
        public EventHandler RequestWarnings = delegate { };


        public ScratchWindow()
        {
            InitializeComponent();
        }

        private void ScratchWindow_Load(object sender, EventArgs e)
        {
            scratch1.SetElements(events);
            scratch1.ElementSelected += new EventHandler(this.EventSelected);
            events.ElementsChanged += this_elementChanged;
        }

        private void this_elementChanged(object sender, EventArgs e)
        {
            if (((ChangedEventArgs)e).change != ChangedType.Selected)  
                RequestWarnings(sender, e);
        }

        public SmartEvent Selected()
        {
            if (!(scratch1.selectedElement is SmartEvent))
                throw new InvalidOperationException();
            return (SmartEvent)scratch1.selectedElement;
        }

        private void EventSelected(object sender, EventArgs e)
        {
            ElementSelected(this, new EventArgs());
        }

        // @TODO: to rewrite ASAP!! just for test
        // I know, it it TOTAL MESS
        public void LoadFromDB(int entryorguid)
        {
            DBConnect connect = new DBConnect();
            bool opened = connect.OpenConnection();

            if (!opened)
            {
                return;
            }

            events.Clear();

            MySql.Data.MySqlClient.MySqlCommand cmd = connect.Query("SELECT * FROM conditions WHERE sourceentry = " + entryorguid + " and sourceid=0 and sourcetypeorreferenceid=22");
            Dictionary<int, List<SmartCondition>> conditions = new Dictionary<int, List<SmartCondition>>();
            
            using (MySql.Data.MySqlClient.MySqlDataReader reader = cmd.ExecuteReader())
            {
                int prevelsegroup = 0;
                while (reader.Read())
                {
                    int id = Convert.ToInt32(reader["sourcegroup"]) - 1;

                    if (!conditions.ContainsKey(id))
                        conditions.Add(id, new List<SmartCondition>());

                    if (Convert.ToInt32(reader["ElseGroup"]) != prevelsegroup)
                        conditions[id].Add(new CONDITION_LOGICAL_OR());

                    SmartCondition cond = ConditionsFactory.Factory(Convert.ToInt32(reader["ConditionTypeOrReference"]));
                    cond.parameters[0].SetValue(Convert.ToInt32(reader["ConditionValue1"]));
                    cond.parameters[1].SetValue(Convert.ToInt32(reader["ConditionValue2"]));
                    cond.parameters[2].SetValue(Convert.ToInt32(reader["ConditionValue3"]));
                    conditions[id].Add(cond);

                    prevelsegroup = Convert.ToInt32(reader["ElseGroup"]);
                }
            }

            cmd = connect.Query("SELECT * FROM smart_scripts WHERE source_type = 0 and entryorguid = "+entryorguid + " order by id");
            SmartEvent prev = null;
            using (MySql.Data.MySqlClient.MySqlDataReader reader = cmd.ExecuteReader())
            {
                int next_link = -1;
                while (reader.Read())
                {
                    //(`entryorguid`,`source_type`,`id`,`link`,`event_type`,`event_phase_mask`,`event_chance`,`event_flags`,`event_param1`,`event_param2`,`event_param3`,`event_param4`,`action_type`,`action_param1`,`action_param2`,`action_param3`,`action_param4`,`action_param5`,`action_param6`,`target_type`,`target_param1`,`target_param2`,`target_param3`,`target_x`,`target_y`,`target_z`,`target_o`,`comment`)
                    int id = Convert.ToInt32(reader["id"]);
                    int entry = Convert.ToInt32(reader["entryorguid"]);
                    SmartAction a = ExtendedFactories.ActionFactory(Convert.ToInt32(reader["action_type"]));
                    int asad = Convert.ToInt32(reader["target_type"]);
                    SmartTarget target = TargetsFactory.Factory(Convert.ToInt32(reader["target_type"]));

                    for (int i = 0; i < 6; i++)
                        a.UpdateParams(i, Convert.ToInt32(reader["action_param" + (i + 1)]));


                    for (int i = 0; i < 3; i++)
                        target.UpdateParams(i, Convert.ToInt32(reader["target_param" + (i + 1)]));

                    target.position[0] = (float)Convert.ToDouble(reader["target_x"]);
                    target.position[1] = (float)Convert.ToDouble(reader["target_y"]);
                    target.position[2] = (float)Convert.ToDouble(reader["target_z"]);
                    target.position[3] = (float)Convert.ToDouble(reader["target_o"]);

                    a.target = target;
                    
                    if (id == next_link)
                    {
                        prev.AddAction(a);
                    }
                    else
                    {
                        SmartEvent ev = ExtendedFactories.EventFactory(Convert.ToInt32(reader["event_type"]));
                        ev.UpdateParams(0, Convert.ToInt32(reader["event_param1"]));
                        ev.UpdateParams(1, Convert.ToInt32(reader["event_param2"]));
                        ev.UpdateParams(2, Convert.ToInt32(reader["event_param3"]));
                        ev.UpdateParams(3, Convert.ToInt32(reader["event_param4"]));
                        ev.chance = Convert.ToInt32(reader["event_chance"]);
                        ev.phasemask = (SmartPhaseMask)Convert.ToInt32(reader["event_phase_mask"]);
                        ev.flags = (SmartEventFlag)Convert.ToInt32(reader["event_flags"]);
                        if (conditions.ContainsKey(id))
                        {
                            foreach(SmartCondition cond in conditions[id])
                            {
                                ev.AddCondition(cond);
                            }
                        }

                        ev.AddAction(a);
                        events.Add(ev);
                        prev = ev;
                    }


                    next_link = Convert.ToInt32(reader["link"]);
                }
            }
            connect.CloseConnection();
            scratch1.Refresh();
        }


        // @TODO: to rewrite
        private void scratch1_DragDrop(object sender, DragEventArgs e)
        {
            Point mouse = scratch1.PointToClient(new Point(e.X, e.Y));

            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                string str = (string)e.Data.GetData(DataFormats.StringFormat);
                SmartEvent el = events.EventAt(mouse.X, mouse.Y);
                DropResult drop_result = events.GetDropResult(str, mouse.X, mouse.Y);
                if (str.IndexOf("SMART_EVENT")>-1)
                {
                    DropEvent(str, mouse);
                }
                else if (str.IndexOf("SMART_ACTION")>-1)
                {
                    if (drop_result == DropResult.INSERT)
                    {
                        el.InsertAction(ExtendedFactories.ActionFactory(str), el.GetInsertActionIndexFromPos(mouse.X, mouse.Y));
                    }
                    else if (drop_result == DropResult.REPLACE)
                    {
                        DrawableElement action = el.GetElementFromPos(mouse.X, mouse.Y);
                        el.ReplaceAction(ExtendedFactories.ActionFactory(str), (SmartAction)action);
                    }
                }
                else if (str.Contains("CONDITION_"))
                {
                    if (drop_result == DropResult.INSERT)
                    {
                        el.InsertCondition(ConditionsFactory.Factory(str), el.GetInsertConditionIndexFromPos(mouse.X, mouse.Y));
                    }
                    else if (drop_result == DropResult.REPLACE)
                    {
                        DrawableElement condition = el.GetElementFromPos(mouse.X, mouse.Y);
                        el.ReplaceCondition(ConditionsFactory.Factory(str), (SmartCondition)condition);
                    }
                }
                // @TODO rewrite
                else if (str.IndexOf("SMART_TARGET") > -1)
                {
                        DrawableContainerElement ev = (DrawableContainerElement)el;
                        SmartAction action = (SmartAction)ev.GetElementFromPos(mouse.X, mouse.Y);
                        SmartTarget target = TargetsFactory.Factory(str);
                        target.UpdateParams(action.target);
                        action.target = target;
                }

            }
        }

        // @TODO: to rewrite ASAP!!
        // I know, it it TOTAL MESS
        private void DropEvent(string strEvent, Point mouse)
        {
            if (events.Count == 0 || mouse.X < 5)
            {
                events.Insert(ExtendedFactories.EventFactory(strEvent), 0);
            }
            else if (mouse.Y > events[events.Count-1].rect.Bottom )
            {
                events.Insert(ExtendedFactories.EventFactory(strEvent), events.Count);
            }
            else
            {
                for (int i = 0; i < events.Count; ++i)
                {
                    SmartEvent ev = events.GetEvent(i);
                    
                    if (mouse.Y > ev.rect.Bottom - 10 && mouse.Y < ev.rect.Bottom + 10)
                    {
                        events.Insert(ExtendedFactories.EventFactory(strEvent), i + 1);
                        break;
                    } 
                    else if (ev.contains(mouse.X, mouse.Y))
                    {
                        SmartEvent new_event = ExtendedFactories.EventFactory(strEvent);
                        new_event.Copy(ev);
                        events.Replace(ev, new_event);
                        break;
                    }
                }
            }
        }

        private void scratch1_DragOver(object sender, DragEventArgs e)
        {
            Point p = scratch1.PointToClient(new Point(e.X, e.Y));
            DropResult drop_result = DropResult.NONE;
            string str = (string)e.Data.GetData(DataFormats.StringFormat);

            drop_result = events.GetDropResult(str, p.X, p.Y);

            if (drop_result == DropResult.INSERT)
                e.Effect = DragDropEffects.Copy;
            else if (drop_result == DropResult.NONE)
                e.Effect = DragDropEffects.None;
            else
                e.Effect = DragDropEffects.Move;

            scratch1.Refresh();
        }

        // @TODO: to rewrite ASAP!!
        // I know, it it TOTAL MESS
        public String GenerateSQLOutput()
        {
            int event_id = 0;
            int entryorguid = 0;
            int source_type = 0;
            StringBuilder resres = new StringBuilder();
            foreach (SmartEvent e in events)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11} ",
                    entryorguid, source_type, event_id, (e.GetActions().Count==1?0:event_id+1), e.ID, (int)e.phasemask, e.chance, (int)e.flags, 
                e.parameters[0].GetValue(), e.parameters[1].GetValue(), e.parameters[2].GetValue(), e.parameters[3].GetValue());

                SmartAction first_action = e.GetAction(0);
                sb.AppendFormat("{0}, ", first_action.ID);
                for (int i = 0; i < 6; i++)
                    sb.AppendFormat("{0}, ", first_action.parameters[i].GetValue());
                sb.AppendFormat("0, 0, 0, 0, 0, 0, 0, \"{0}\"),", e + " => " + first_action.ToString());
                resres.AppendLine(sb.ToString());

                event_id++;

                for (int index = 1; index < e.GetActions().Count; ++index)
                {
                    SmartAction action = e.GetAction(index);
                    StringBuilder sb2 = new StringBuilder();
                    sb2.AppendFormat("({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11} ",
                    entryorguid, source_type, event_id, (e.GetActions().Count-1==index?0:event_id+1), 61, 0, 100, 0, 
                        0,0,0,0);
                    sb2.AppendFormat("{0}, ", action.ID);
                    for (int i = 0; i < 6; i++)
                        sb2.AppendFormat("{0}, ", action.parameters[i].GetValue());
                    sb2.AppendFormat("0, 0, 0, 0, 0, 0, 0, \"{0}\"),", e + " => " + action.ToString());
                    resres.AppendLine(sb2.ToString());
                    event_id++;
                }
            }
            return resres.ToString();
        }

        public void DetectConflicts()
        {
            StringBuilder conflicts = new StringBuilder();

            for (int i = 0; i < events.Count;++i )
            {
                SmartEvent ev = events.GetEvent(i);

                for (int j = 0; j < ev.GetActions().Count; ++j)
                {
                    SmartAction a1 = ev.GetAction(j);
                    SmartAction a2 = ActionsFactory.Factory(a1.ID);
                    a2.Copy(a1);

                    for (int p = 0; p < 6;++p )
                    {
                        if (a1.parameters[p].GetType() != a2.parameters[p].GetType())
                            conflicts.AppendLine("Instead of parameter: " + a1.parameters[p].GetType() + "\nWe have: " + a2.parameters[p].GetType());
                    }

                        if (a2.ToString() != a1.ToString())
                            conflicts.AppendLine("Instead of: \n " + a1.ToString() + "\nWe have:\n " + a2.ToString());
                }

            }

                if (conflicts.Length > 0)
                    MessageBox.Show(conflicts.ToString());

        }

        public IEnumerable<SmartEvent> GetEvents()
        {
            return events.collection.Cast<SmartEvent>().ToList();
        }

        public void EnsureVisible(DrawableElement drawableElement)
        {
            scratch1.EnsureVisible(drawableElement);
        }

        private void scratch1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
