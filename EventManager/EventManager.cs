using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace WManager
{
    /// <summary>
    /// 事件管理器
    /// </summary>
    public class EventManager
    {

        #region " 变量 "

        private static Dictionary<string, UnityEvent> eventDictionary = new Dictionary<string, UnityEvent>();
        private static Dictionary<string, object> sender = new Dictionary<string, object>();
        private static Dictionary<string, bool> paused = new Dictionary<string, bool>();

        // 包含随发出的事件传递的数据的内存存储器。
        private static Dictionary<string, object> storage = new Dictionary<string, object>();

        // DataGroup 方法使用的内存存储。
        private static Dictionary<string, object[]> storage2 = new Dictionary<string, object[]>();

        // IndexedDataGroup 方法使用的内存存储。
        private static Dictionary<string, DataGroup[]> storage3 = new Dictionary<string, DataGroup[]>();

        // 管理与callBack函数关联的惟一id，以便使用id作为引用，而不是回调函数。
        private static Dictionary<string, UnityAction> callBacks = new Dictionary<string, UnityAction>();

        private struct SFilter
        {
            public string value;
            public bool starts;
            public bool ends;
            public bool contains;
            public bool exact;
        }

        #endregion

        #region " 开始监听 "

        /// <summary>
        /// 以给定的名称开始监听事件。如果检测到该事件，则执行callBack函数。你也可以在callBackID参数中定义一个唯一的ID。
        /// </summary>
        /// <param name="eventName">要监听的事件名称</param>
        /// <param name="callBack">每次检测到此事件时要调用的函数的名称</param>
        /// <param name="callBackID">指定后，回调函数由此唯一 ID 标识。可以在 StopListening()方法中使用此 ID 字符串，而不是使用 callBack 函数名称</param>
        public static void StartListening(string eventName, UnityAction callBack, string callBackID = "")
        {
            if (eventDictionary.TryGetValue(eventName, out UnityEvent thisEvent))
            {
                thisEvent.AddListener(callBack);
            }
            else
            {
                thisEvent = new UnityEvent();
                thisEvent.AddListener(callBack);
                eventDictionary.Add(eventName, thisEvent);
                paused.Add(eventName, false);
            }

            if (callBackID != "") callBacks.Add(eventName + "_" + callBackID, callBack);
        }

        /// <summary>
        /// 开始监听具有给定名称的事件，并在事件发射上启用过滤器。如果检测到该事件，并且满足可选过滤器，则执行callBack函数。你也可以在callBackID参数中定义一个唯一的ID。
        /// </summary>
        /// <param name="eventName">要监听的事件名称</param>
        /// <param name="target">正在监听此事件的GameObject。开启过滤功能则必须定义。脚本所在物体为gameObject</param>
        /// <param name="callBack">每次检测到此事件时要调用的函数的名称</param>
        /// <param name="callBackID">指定后，回调函数由此唯一 ID 标识。可以在 StopListening()方法中使用此 ID 字符串，而不是使用 callBack 函数名称</param>
        public static void StartListening(string eventName, GameObject target, UnityAction callBack, string callBackID = "")
        {
            if (target == null)
            {
                Debug.LogError("指定的目标不是一个有效的游戏对象");
                return;
            }

            StartListening(eventName, callBack);
            if (callBackID != "") callBacks.Add(eventName + "_" + callBackID, callBack);

            string newName = eventName + "__##name##" + target.name + "##" + "__##tag##" + target.tag + "##" + "__##layer##" + target.layer + "##";
            StartListening(newName, callBack);
            if (callBackID != "") callBacks.Add(eventName + "_" + callBackID + "_EXTRA", callBack);

        }

        #endregion

        #region " 停止监听 "

        /// <summary>
        /// 停止监听具有给定名称的事件，由该事件占用的内存将被清除。callBack函数必须通过在StartListening()方法中设置具有唯一ID的“callBackID”参数来保存。
        /// </summary>
        /// <param name="eventName">不想再监听的事件名称</param>
        /// <param name="callBackID">与回调函数关联的唯一ID</param>
        public static void StopListening(string eventName, string callBackID)
        {
            if (eventDictionary.TryGetValue(eventName, out UnityEvent thisEvent))
            {
                if (callBackID != "")
                {
                    if (callBacks.ContainsKey(eventName + "_" + callBackID))
                    {
                        thisEvent.RemoveListener(callBacks[eventName + "_" + callBackID]);
                    }
                    if (callBacks.ContainsKey(eventName + "_" + callBackID + "_EXTRA"))
                    {
                        thisEvent.RemoveListener(callBacks[eventName + "_" + callBackID + "_EXTRA"]);
                    }
                }
            }
        }

        /// <summary>
        /// 停止监听具有给定名称的事件，由该事件占用的内存将被清除。必须指定callBack函数。
        /// </summary>
        /// <param name="eventName">不想再监听的事件名称</param>
        /// <param name="callBack">被调用的回调函数的名称</param>
        public static void StopListening(string eventName, UnityAction callBack)
        {
            if (eventDictionary.TryGetValue(eventName, out UnityEvent thisEvent))
            {
                thisEvent.RemoveListener(callBack);
            }
        }

        #endregion

        #region " 发出事件 "

        /// <summary>
        /// 发出具有给定名称的事件
        /// </summary>
        /// <param name="eventName">要监听的事件的名称</param>
        public static void EmitEvent(string eventName)
        {
            if (isPaused(eventName)) return;

            if (eventDictionary.TryGetValue(eventName, out UnityEvent thisEvent))
            {
                thisEvent.Invoke();
            }
        }

        /// <summary>
        /// 发出具有给定名称的事件并保存发送对象
        /// </summary>
        /// <param name="eventName">要监听的事件的名称</param>
        /// <param name="sender">发出此事件的对象</param>
        public static void EmitEvent(string eventName, object sender)
        {
            if (isPaused(eventName)) return;

            if (EventManager.sender.ContainsKey(eventName)) EventManager.sender[eventName] = sender; else EventManager.sender.Add(eventName, sender);

            EmitEvent(eventName);
        }

        /// <summary>
        /// 在指定的延迟秒之后发出具有给定名称的事件
        /// </summary>
        /// <param name="eventName">要监听的事件的名称</param>
        /// <param name="delay">发出此事件之前要等待的秒数</param>
        public static void EmitEvent(string eventName, float delay)
        {
            if (isPaused(eventName)) return;

            if (eventDictionary.TryGetValue(eventName, out UnityEvent thisEvent))
            {
                if (delay <= 0)
                {
                    thisEvent.Invoke();
                }
                else
                {
                    int d = (int)(delay * 1000);
                    DelayedInvoke(thisEvent, d);
                }
            }
        }

        /// <summary>
        /// 在指定的延迟秒后发出具有给定名称的事件，并保存发送对象
        /// </summary>
        /// <param name="eventName">要监听的事件的名称</param>
        /// <param name="delay">发出此事件之前要等待的秒数</param>
        /// <param name="sender">发出此事件的对象</param>
        public static void EmitEvent(string eventName, float delay, object sender)
        {
            if (isPaused(eventName)) return;

            if (EventManager.sender.ContainsKey(eventName)) EventManager.sender[eventName] = sender; else EventManager.sender.Add(eventName, sender);

            EmitEvent(eventName, delay);
        }

        /// <summary>
        /// 将具有给定名称的事件发送给过滤器指定的监听器。可选：可以指定延迟和发送者。
        /// </summary>
        /// <param name="eventName">要监听的事件的名称</param>
        /// <param name="filter">过滤器。用;隔开，name:GameObject的名称，tag:标签名，layer:图层序号。用*来表示其它存在的文本</param>
        /// <param name="delay">发出此事件之前要等待的秒数</param>
        /// <param name="sender">发出此事件的对象</param>
        public static void EmitEvent(string eventName, string filter, float delay = 0f, object sender = null)
        {
            if (sender != null)
            {
                if (EventManager.sender.ContainsKey(eventName)) EventManager.sender[eventName] = sender; else EventManager.sender.Add(eventName, sender);
            }

            // 提取过滤数据。
            var data = filter.Split(';');
            var filters = new Dictionary<string, SFilter>();

            foreach (string s in data)
            {
                var tmp = s.Split(':');
                if (tmp[0] == "name") filters.Add("name", new SFilter { value = tmp[1].Replace("*", ""), contains = tmp[1].StartsWith("*") && tmp[1].EndsWith("*"), starts = tmp[1].EndsWith("*"), ends = tmp[1].StartsWith("*"), exact = !tmp[1].Contains("*") });
                if (tmp[0] == "tag") filters.Add("tag", new SFilter { value = tmp[1].Replace("*", ""), contains = tmp[1].StartsWith("*") && tmp[1].EndsWith("*"), starts = tmp[1].EndsWith("*"), ends = tmp[1].StartsWith("*"), exact = !tmp[1].Contains("*") });
                if (tmp[0] == "layer") filters.Add("layer", new SFilter { value = tmp[1].Replace("*", ""), contains = tmp[1].StartsWith("*") && tmp[1].EndsWith("*"), starts = tmp[1].EndsWith("*"), ends = tmp[1].StartsWith("*"), exact = !tmp[1].Contains("*") });
            }

            int counter = filters.Count;
            int found = 0;

            // 搜索要发出的有效事件。

            foreach (KeyValuePair<string, UnityEvent> evnt in eventDictionary)
            {
                var key = evnt.Key;

                if (key.Contains("_") && key.StartsWith(eventName))
                {
                    data = key.Split('_');

                    var name = "";
                    var tag = "";
                    var layer = "";

                    found = 0;

                    foreach (string s in data)
                    {
                        if (s.Contains("##name##")) name = s.Replace("##name##", "").Replace("#", "");
                        if (s.Contains("##tag##")) tag = s.Replace("##tag##", "").Replace("#", "");
                        if (s.Contains("##layer##")) layer = s.Replace("##layer##", "").Replace("#", "");
                    }

                    if (filters.ContainsKey("name") && name != "")
                    {
                        if (FilterIsValidated(name, filters["name"])) found++;
                    }
                    if (filters.ContainsKey("tag") && tag != "")
                    {
                        if (FilterIsValidated(tag, filters["tag"])) found++;
                    }
                    if (filters.ContainsKey("layer") && layer != "")
                    {
                        if (FilterIsValidated(layer, filters["layer"])) found++;
                    }

                    if (found == counter) { EmitEvent(key, delay); }
                }

            }

        }

        private static bool FilterIsValidated(string value, SFilter rules)
        {
            if (rules.exact)
            {
                return value == rules.value;
            }
            else if (rules.contains)
            {
                return value.Contains(rules.value);
            }
            else if (rules.starts)
            {
                return value.StartsWith(rules.value);
            }
            else if (rules.ends)
            {
                return value.EndsWith(rules.value);
            }
            return false;
        }

        /// <summary>
        /// 发出具有给定名称和数据的事件（可选，有延迟）。
        /// </summary>
        /// <param name="eventName">要监听的事件的名称</param>
        /// <param name="data">数据</param>
        /// <param name="delay">发出此事件之前要等待的秒数</param>
        public static void EmitEventData(string eventName, object data, float delay = 0f)
        {
            SetData(eventName, data);
            EmitEvent(eventName, delay);
        }

        #endregion

        #region " 实用方法 "

        /// <summary>
        /// 停止所有监听事件
        /// </summary>
        public static void StopAll()
        {
            foreach (KeyValuePair<string, UnityEvent> evnt in eventDictionary)
            {
                evnt.Value.RemoveAllListeners();
            }
            eventDictionary = new Dictionary<string, UnityEvent>();
        }

        private static async void DelayedInvoke(UnityEvent thisEvent, int delay)
        {
            await Task.Delay(delay);
            thisEvent.Invoke();
        }

        /// <summary>
        /// 如果至少有一个监听器，则返回true
        /// </summary>
        /// <returns></returns>
        public static bool IsListening()
        {
            return eventDictionary.Count > 0;
        }

        /// <summary>
        /// 暂停监听器
        /// </summary>
        public static void PauseListening()
        {
            SetPaused(true);
        }

        /// <summary>
        /// 用给定的名称暂停事件的监听。
        /// </summary>
        /// <param name="eventName">要暂停的监听事件名</param>
        public static void PauseListening(string eventName)
        {
            SetPaused(eventName, true);
        }

        /// <summary>
        /// 重新启动监听。
        /// </summary>
        public static void RestartListening()
        {
            SetPaused(false);
        }

        /// <summary>
        /// 用给定的名称重新启动事件监听。
        /// </summary>
        /// <param name="eventName">要重启的监听事件名</param>
        public static void RestartListening(string eventName)
        {
            SetPaused(eventName, false);
        }

        /// <summary>
        /// 如果给定名称的事件已经暂停，则返回true。
        /// </summary>
        /// <param name="eventName">要判断是否暂停的监听事件名</param>
        /// <returns></returns>
        public static bool isPaused(string eventName)
        {
            if (paused.ContainsKey(eventName)) return paused[eventName]; else return true;
        }

        private static void SetPaused(bool value)
        {
            Dictionary<string, bool> copy = new Dictionary<string, bool>();

            foreach (KeyValuePair<string, bool> eName in paused)
            {
                copy.Add(eName.Key, value);
            }

            paused = copy;
        }

        private static void SetPaused(string eventName, bool value)
        {
            Dictionary<string, bool> copy = new Dictionary<string, bool>();

            foreach (KeyValuePair<string, bool> eName in paused)
            {
                if (eName.Key == eventName) copy.Add(eName.Key, value); else copy.Add(eName.Key, eName.Value);
            }

            paused = copy;
        }

        /// <summary>
        /// 如果给定名称的事件存在，则返回true。
        /// </summary>
        public static bool EventExists(string eventName)
        {
            return eventDictionary.ContainsKey(eventName);
        }

        /// <summary>
        /// [DEPRECATED] Use Dispose() or DisposeAll().
        /// </summary>
        // public static void ClearData()
        // {
        //     storage = new Dictionary<string, object>();
        //     sender = new Dictionary<string, object>();
        // }

        /// <summary>
        /// 清除指定事件名称占用的内存。此方法仅清除数据，而侦听器继续工作。
        /// </summary>
        /// <param name="eventName">清除指定事件名称</param>
        public static void Dispose(string eventName)
        {
            if (storage.ContainsKey(eventName)) storage.Remove(eventName);
            if (storage2.ContainsKey(eventName)) storage2.Remove(eventName);
            if (storage3.ContainsKey(eventName)) storage3.Remove(eventName);
        }

        /// <summary>
        /// 清除事件管理器系统占用的内存。此方法仅清除数据，而侦听器继续工作。
        /// </summary>
        public static void DisposeAll()
        {
            storage.Clear();
            storage2.Clear();
            storage3.Clear();
            sender.Clear();
        }

        #endregion

        #region " 获取和设置数据 "

        /// <summary>
        /// 为给定名称的事件保存数据。
        /// </summary>
        /// <param name="eventName">要发出的事件的名称。</param>
        /// <param name="data">与此事件关联的数据。</param>
        public static void SetData(string eventName, object data)
        {
            if (storage.ContainsKey(eventName)) storage[eventName] = data; else storage.Add(eventName, data);
        }

        /// <summary>
        /// 返回具有给定名称的事件数据(如果没有找到，则为null)。
        /// </summary>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public static object GetData(string eventName)
        {
            try
            {
                if (storage.ContainsKey(eventName)) return storage[eventName]; else return null;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 返回带有给定名称的事件的GameObject数据(如果没有找到，则为null)。
        /// </summary>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public static GameObject GetGameObject(string eventName)
        {
            try
            {
                if (storage.ContainsKey(eventName)) return (GameObject)storage[eventName]; else return null;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 返回具有给定名称的事件的整数数据(如果没有找到，则返回0)。
        /// </summary>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public static int GetInt(string eventName)
        {
            try
            {
                if (storage.ContainsKey(eventName)) return (int)storage[eventName]; else return 0;
            }
            catch (System.Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// 返回具有给定名称的事件的布尔数据(如果没有找到，则为false)。
        /// </summary>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public static bool GetBool(string eventName)
        {
            try
            {
                if (storage.ContainsKey(eventName)) return (bool)storage[eventName]; else return false;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 返回具有给定名称的事件的浮动数据(如果没有找到，则为0)。
        /// </summary>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public static float GetFloat(string eventName)
        {
            try
            {
                if (storage.ContainsKey(eventName)) return (float)storage[eventName]; else return 0f;
            }
            catch (System.Exception)
            {
                return 0f;
            }
        }

        /// <summary>
        /// 返回具有给定名称的事件的字符串数据(如果没有找到，则返回"")。
        /// </summary>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public static string GetString(string eventName)
        {
            try
            {
                if (storage.ContainsKey(eventName)) return (string)storage[eventName]; else return "";
            }
            catch (System.Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// 返回具有给定名称的事件的发送方(如果没有找到，则为null)。
        /// </summary>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public static object GetSender(string eventName)
        {
            try
            {
                if (sender.ContainsKey(eventName)) return sender[eventName]; else return null;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        #endregion

        #region " 获取和设置数据组 "

        public struct DataGroup
        {
            /// <summary>
            /// 原始对象数据。
            /// </summary>
            public object data;

            /// <summary>
            /// 此数据组的唯一标识符。
            /// </summary>
            public string id;

            /// <summary>
            /// 将对象转换为游戏对象。
            /// </summary>
            /// <returns></returns>
            public GameObject ToGameObject()
            {
                try
                {
                    return (GameObject)data;
                }
                catch (System.Exception)
                {
                    return null;
                }
            }

            /// <summary>
            /// 将对象转换为整数值。
            /// </summary>
            /// <returns></returns>
            public int ToInt()
            {
                try
                {
                    return (int)data;
                }
                catch (System.Exception)
                {
                    return 0;
                }
            }

            /// <summary>
            /// 将对象转换为浮点值。
            /// </summary>
            /// <returns></returns>
            public float ToFloat()
            {
                try
                {
                    return (float)data;
                }
                catch (System.Exception)
                {
                    return 0f;
                }
            }

            /// <summary>
            /// 将对象转换为字符串。
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                try
                {
                    return (string)data;
                }
                catch (System.Exception)
                {
                    return "";
                }
            }

            /// <summary>
            /// 将对象转换为布尔值。
            /// </summary>
            /// <returns></returns>
            public bool ToBool()
            {
                try
                {
                    return (bool)data;
                }
                catch (System.Exception)
                {
                    return false;
                }
            }

        }

        /// <summary>
        /// 为给定名称的Event保存一组数据。然后使用getdatgroup方法来访问该数据。
        /// </summary>
        public static void SetDataGroup(string eventName, params object[] data)
        {
            if (storage3.ContainsKey(eventName)) { Debug.LogWarning(eventName + " Event name is already in use with DataGroup."); return; }
            if (storage2.ContainsKey(eventName)) storage2[eventName] = data; else storage2.Add(eventName, data);
        }

        /// <summary>
        /// 返回一个包含所有事件数据的结构化数组，如果没有找到任何数据，则返回null。
        /// </summary>
        public static DataGroup[] GetDataGroup(string eventName)
        {
            if (storage2.ContainsKey(eventName))
            {

                var strg = storage2[eventName];
                DataGroup[] objList = new DataGroup[strg.Length];

                for (var i = 0; i < strg.Length; i++)
                {
                    objList[i] = new DataGroup { data = strg[i] };
                }

                return objList;

            }

            return null;
        }


        #endregion

        #region " 获取和设置索引数据组 "

        public struct IndexedDataGroup
        {

            public DataGroup[] data;

            private object objectData;

            /// <summary>
            /// 返回原始对象。
            /// </summary>
            /// <param name="id"></param>
            /// <returns></returns>
            public object GetObject(string id)
            {
                return null;
            }

            /// <summary>
            /// 将对象转换为游戏对象。
            /// </summary>
            /// <returns></returns>
            public GameObject ToGameObject(string id)
            {
                objectData = Find(id);

                try
                {
                    return (GameObject)objectData;
                }
                catch (System.Exception)
                {
                    return null;
                }
            }

            /// <summary>
            /// 将对象转换为整数值。
            /// </summary>
            /// <returns></returns>
            public int ToInt(string id)
            {
                objectData = Find(id);

                try
                {
                    return (int)objectData;
                }
                catch (System.Exception)
                {
                    return 0;
                }
            }

            /// <summary>
            /// 将对象转换为浮点值。
            /// </summary>
            /// <returns></returns>
            public float ToFloat(string id)
            {
                objectData = Find(id);

                try
                {
                    return (float)objectData;
                }
                catch (System.Exception)
                {
                    return 0f;
                }
            }

            /// <summary>
            /// 将对象转换为字符串。
            /// </summary>
            /// <returns></returns>
            public string ToString(string id)
            {
                objectData = Find(id);

                try
                {
                    return (string)objectData;
                }
                catch (System.Exception)
                {
                    return "";
                }
            }

            /// <summary>
            /// 将对象转换为布尔值。
            /// </summary>
            /// <returns></returns>
            public bool ToBool(string id)
            {
                objectData = Find(id);

                try
                {
                    return (bool)objectData;
                }
                catch (System.Exception)
                {
                    return false;
                }
            }

            /// <summary>
            /// 如果没有数据则返回true。
            /// </summary>
            /// <returns></returns>
            public bool IsEmpty()
            {
                return data.Length == 0;
            }

            private object Find(string id)
            {
                foreach (var obj in data) if (obj.id == id) return obj.data;
                return null;
            }

        }

        /// <summary>
        ///为给定名称的事件保存一组DataGroups。然后使用getindexeddataggroup方法来访问这类数据。
        /// </summary>
        public static void SetIndexedDataGroup(string eventName, params DataGroup[] data)
        {
            if (storage2.ContainsKey(eventName)) { Debug.LogWarning(eventName + " Event name is already in use with DataGroup."); return; }
            if (storage3.ContainsKey(eventName)) storage3[eventName] = data; else storage3.Add(eventName, data);
        }

        /// <summary>
        /// 返回包含所有事件数据的结构化数据组，如果没有找到任何数据，则返回null。
        /// </summary>
        public static IndexedDataGroup GetIndexedDataGroup(string eventName)
        {
            if (storage3.ContainsKey(eventName))
            {

                var strg = storage3[eventName];

                IndexedDataGroup data = new IndexedDataGroup();
                data.data = strg;

                return data;

            }

            return new IndexedDataGroup();
        }

        #endregion

    }

    #region " 事件组 "

    /// <summary>
    /// 创建一组事件，可以启动和停止执行。
    /// </summary>
    public class EventsGroup
    {

        private struct SEvent
        {
            public string name;
            public UnityAction callBack;
        }

        private List<SEvent> group = new List<SEvent>();

        /// <summary>
        /// 向EventsGroup添加一个新的监听器。
        /// </summary>
        public void Add(string eventName, UnityAction callBack)
        {
            group.Add(new SEvent { name = eventName, callBack = callBack });
        }

        /// <summary>
        /// 开始收听组中的所有事件。
        /// </summary>
        public void StartListening()
        {
            foreach (SEvent g in group)
            {
                EventManager.StartListening(g.name, g.callBack);
            }
        }

        /// <summary>
        /// 停止收听组中的所有事件。如果指定了eventName，则只停止该事件。
        /// </summary>
        public void StopListening(string eventName = "")
        {
            if (eventName == "")
            {
                foreach (SEvent g in group)
                {
                    EventManager.StopListening(g.name, g.callBack);
                }
            }
            else
            {
                List<SEvent> newGroup = new List<SEvent>();
                foreach (SEvent g in group)
                {
                    if (g.name != eventName) newGroup.Add(g); else EventManager.StopListening(g.name, g.callBack);
                }
                group = newGroup;
            }

        }

        /// <summary>
        /// 如果EventsGroup包含给定名称的事件，则返回true。
        /// </summary>
        public bool Contains(string eventName)
        {
            foreach (SEvent g in group)
            {
                if (g.name == eventName) return true;
            }
            return false;
        }

    }

    #endregion



}
