﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualSAIStudio
{
    class ExtendedFactories
    {
        public static Dictionary<String, SmartGenericJSONData> action_name_data = new Dictionary<string, SmartGenericJSONData>();
        public static Dictionary<int, SmartGenericJSONData> action_id_data = new Dictionary<int,SmartGenericJSONData>();

        public static Dictionary<String, SmartGenericJSONData> event_name_data = new Dictionary<string, SmartGenericJSONData>();
        public static Dictionary<int, SmartGenericJSONData> event_id_data = new Dictionary<int, SmartGenericJSONData>();


        public static void AddAction(SmartGenericJSONData data)
        {
            action_name_data.Add(data.name, data);
            action_id_data.Add(data.id, data);
        }

        public static void AddEvent(SmartGenericJSONData data)
        {
            event_name_data.Add(data.name, data);
            event_id_data.Add(data.id, data);
        }

        public static SmartEvent EventFactory(string id)
        {
            if (event_name_data.ContainsKey(id))
                return GenericSmartEvent.Factory(event_name_data[id]);
            return EventsFactory.Factory(id);
        }

        public static SmartEvent EventFactory(int id)
        {
            if (event_id_data.ContainsKey(id))
                return GenericSmartEvent.Factory(event_id_data[id]);
            return EventsFactory.Factory(id);
        }

        public static SmartAction ActionFactory(string id)
        {
            if (action_name_data.ContainsKey(id))
                return GenericSmartAction.Factory(action_name_data[id]);
            return ActionsFactory.Factory(id);
        }

        public static SmartAction ActionFactory(int id)
        {
            if (action_id_data.ContainsKey(id))
                return GenericSmartAction.Factory(action_id_data[id]);
            return ActionsFactory.Factory(id);
        }
    }
}
