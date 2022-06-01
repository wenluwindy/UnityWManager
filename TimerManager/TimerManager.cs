using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WManager
{
    public class Timer
    {
        static List<Timer> timers = new List<Timer>();
        Action<float> UpdateEvent;
        Action EndEvent;
        /// <summary>
        /// 用户设定的定时时长
        /// </summary>
        private float _time = -1;
        /// <summary>
        /// 是否循环执行
        /// </summary>
        private bool _loop;
        /// <summary>
        /// 是否忽略时间尺度Timescale
        /// </summary>
        private bool _ignorTimescale;
        /// <summary>
        /// 用户指定的定时器标志，便于手动清除、暂停、恢复
        /// </summary>
        private string _flag;

        private static TimerDriver driver = null;//拿驱动器的引用只是为了初始化驱动器
        /// <summary>
        /// 获得当前时间
        /// </summary>
        private float CurrentTime { get { return _ignorTimescale ? Time.realtimeSinceStartup : Time.time; } }

        private float cachedTime;//缓存时间
        float timePassed;        //已经过去的时间

        private bool _isFinish = false; //计时器是否结束
        private bool _isPause = false; //计时器是否暂停

        private static bool showLog = true;
        public static bool ShowLog { set { showLog = value; } }//确认是否输出Debug信息
        /// <summary>
        /// 暂停计时器
        /// </summary>
        public bool IsPause
        {
            get { return _isPause; }
            set
            {
                if (value)
                {
                    Pause();
                }
                else
                {
                    Resum();
                }
            }

        }
        /// <summary>
        /// 构造定时器
        /// </summary>
        /// <param name="time">定时时长</param>
        /// <param name="flag">定时器标识符</param>
        /// <param name="loop">是否循环</param>
        /// <param name="ignorTimescale">是否忽略TimeScale</param>
        private Timer(float time, string flag, bool loop = false, bool ignorTimescale = true)
        {
            if (null == driver) driver = TimerDriver.Get; //初始化Time驱动
            _time = time;
            _loop = loop;
            _ignorTimescale = ignorTimescale;
            cachedTime = CurrentTime;
            if (timers.Exists((v) => { return v._flag == flag; }))
            {
                if (showLog) Debug.LogWarningFormat("【TimerTrigger（容错）】:存在相同的标识符【{0}】！", flag);
            }
            _flag = string.IsNullOrEmpty(flag) ? GetHashCode().ToString() : flag;//设置辨识标志符
        }

        /// <summary>  
        /// 暂停计时  
        /// </summary>  
        private void Pause()
        {
            if (_isFinish)
            {
                if (showLog) Debug.LogWarning("【TimerTrigger（容错）】:计时已经结束！");
            }
            else
            {
                _isPause = true;
            }
        }
        /// <summary>  
        /// 继续计时  
        /// </summary>  
        private void Resum()
        {
            if (_isFinish)
            {
                if (showLog) Debug.LogWarning("【TimerTrigger（容错）】:计时已经结束！");
            }
            else
            {
                if (_isPause)
                {
                    cachedTime = CurrentTime-timePassed; 
                    _isPause = false;
                }
                else
                {
                    if (showLog) Debug.LogWarning("【TimerTrigger（容错）】:计时并未处于暂停状态！");
                }
            }
        }
        /// <summary>
        /// 刷新定时器
        /// </summary>
        private void Update()
        {
            if (!_isFinish && !_isPause) //运行中
            {
                timePassed = CurrentTime - cachedTime;
                if (null != UpdateEvent) UpdateEvent(Mathf.Clamp01(timePassed / _time));
                if (timePassed >= _time)
                {
                    if (null != EndEvent) EndEvent();
                    if (_loop)
                    {
                        cachedTime = CurrentTime;
                    }
                    else
                    {
                        Stop();
                    }
                }
            }
        }

        /// <summary>
        /// 回收定时器
        /// </summary>
        private void Stop()
        {
            if (timers.Contains(this))
            {
                timers.Remove(this);
            }
            _time = -1;
            _isFinish = true;
            _isPause = false;
            UpdateEvent = null;
            EndEvent = null;
        }



        #region--------------------------静态函数扩展-------------------------------------
        #region-------------添加定时器---------------
        /// <summary>
        /// 添加定时触发器
        /// </summary>
        /// <param name="time">定时时长</param>
        /// <param name="flag">定时器标识符</param>
        /// <param name="loop">是否循环</param>
        /// <param name="ignorTimescale">是否忽略时间尺度(Time Scale)</param>
        /// <returns></returns>
        public static Timer AddTimer(float time, string flag = "", bool loop = false, bool ignorTimescale = true)
        {
            Timer timer = new Timer(time, flag, loop, ignorTimescale);
            timers.Add(timer);
            return timer;
        }
        #endregion

        #region-------------更新所有计时器---------------
        /// <summary>
        /// 更新所有计时器
        /// </summary>
        public static void UpdateAllTimer()
        {
            for (int i = 0; i < timers.Count; i++)
            {
                if (null != timers[i])
                {
                    timers[i].Update();
                }
            }
        }
        #endregion

        #region-------------暂停和恢复计时器---------------
        /// <summary>
        /// 暂停用户指定的计时触发器
        /// </summary>
        /// <param name="flag">指定的标识符</param>
        public static void PauseTimer(string flag)
        {
            Timer timer = timers.Find((v) => { return v._flag == flag; });
            if (null != timer)
            {
                timer.Pause();
            }
            else
            {
                if (showLog) Debug.Log("【TimerTrigger（容错）】:定时器已完成触发或无此定时器！---Flag【" + flag + "】。");
            }
        }
        /// <summary>
        /// 恢复用户指定的计时触发器
        /// </summary>
        /// <param name="flag">指定的标识符</param>
        public static void ResumTimer(string flag)
        {
            Timer timer = timers.Find((v) => { return v._flag == flag; });
            if (null != timer)
            {
                timer.Resum();
            }
            else
            {
                if (showLog) Debug.Log("【TimerTrigger（容错）】:定时器已完成触发或无此定时器！---Flag【" + flag + "】。");
            }
        }

        #endregion
        #region-------------删除计时器---------------
        /// <summary>
        /// 删除用户指定的计时触发器
        /// </summary>
        /// <param name="flag">指定的标识符</param>
        public static void DelTimer(string flag)
        {
            Timer timer = timers.Find((v) => { return v._flag == flag; });
            if (null != timer)
            {
                timer.Stop();
            }
            else
            {
                if (showLog) Debug.Log("【TimerTrigger（容错）】:定时器已完成触发或无此定时器！---Flag【" + flag + "】。");
            }
        }
        /// <summary>
        /// 删除用户指定的计时触发器
        /// </summary>
        /// <param name="flag">指定的定时器</param>
        public static void DelTimer(Timer timer)
        {
            if (timers.Contains(timer))
            {
                timer.Stop();
            }
            else
            {
                if (showLog) Debug.Log("【TimerTrigger（容错）】:此定时器已完成触发或无此定时器！");
            }
        }
        /// <summary>
        /// 删除用户指定的计时触发器
        /// </summary>
        /// <param name="completedEvent">指定的完成事件(直接赋值匿名函数无效)</param>
        public static void DelTimer(Action completedEvent)
        {
            Timer timer = timers.Find((v) => { return v.EndEvent == completedEvent; });
            if (null != timer)
            {
                timer.Stop();
            }
            else
            {
                if (showLog) Debug.Log("【TimerTrigger（容错）】:定时器已完成触发或无此定时器！---方法名：【" + completedEvent.Method.Name + "】。");
            }
        }
        /// <summary>
        /// 删除用户指定的计时触发器
        /// </summary>
        /// <param name="updateEvent">指定的Update事件(直接赋值匿名函数无效)</param>
        public static void DelTimer(Action<float> updateEvent)
        {
            Timer timer = timers.Find((v) => { return v.UpdateEvent == updateEvent; });
            if (null != timer)
            {
                timer.Stop();
            }
            else
            {
                if (showLog) Debug.Log("【TimerTrigger（容错）】:定时器已完成触发或无此定时器！---方法名：【" + updateEvent.Method.Name + "】。");
            }
        }
        #endregion
        #endregion

        #region-------------添加事件-------------------
        /// <summary>
        /// 计时器完成后执行的事件
        /// </summary>
        /// <param name="completedEvent">执行的事件方法</param>
        /// <returns></returns>
        public Timer OnCompleted(Action completedEvent)
        {
            if (null == EndEvent)
            {
                EndEvent = completedEvent;
            }
            else
            {
                if (showLog) Debug.Log("【TimerTrigger（容错）】:定时器已注册了Completed事件,请整合！");
            }
            return this;
        }
        /// <summary>
        /// 计时器完成后每帧执行的事件
        /// </summary>
        /// <param name="updateEvent">执行的事件方法</param>
        /// <returns></returns>
        public Timer OnUpdated(Action<float> updateEvent)
        {
            if (null == UpdateEvent)
            {
                UpdateEvent = updateEvent;
            }
            else
            {
                if (showLog) Debug.Log("【TimerTrigger（容错）】:定时器已注册了Update事件，请整合！");
            }
            return this;
        }

        #endregion

        #region ---------------运行中的定时器参数修改-----------
        /// <summary>
        /// 设置运行中的定时器的loop状态
        /// </summary>
        /// <param name="loop"></param>
        public void Setloop(bool loop)
        {
            if (!_isFinish)
            {
                _loop = loop;
            }
            else
            {
                if (showLog) Debug.Log("【TimerTrigger（容错）】:定时器已失效,设置Loop Fail！");
            }
        }

        /// <summary>
        /// 设置运行中的定时器的ignoreTimescale状态
        /// </summary>
        /// <param name="loop"></param>
        public void SetIgnoreTimeScale(bool ignoreTimescale)
        {
            if (!_isFinish)
            {
                _ignorTimescale = ignoreTimescale;
            }
            else
            {
                if (showLog) Debug.Log("【TimerTrigger（容错）】:定时器已失效，设置IgnoreTimescale Fail！");
            }
        }

        #endregion

    }

    public class TimerDriver : MonoBehaviour
    {
        #region ----------------单例---------------------
        private static TimerDriver _instance;
        public static TimerDriver Get
        {
            get
            {
                if (null == _instance)
                {
                    _instance = FindObjectOfType<TimerDriver>() ?? new GameObject("计时器").AddComponent<TimerDriver>();
                }
                return _instance;
            }
            private set { _instance = value; }
        }
        private void Awake()
        {
            _instance = this;
        }
        #endregion
        private void Update()
        {
            Timer.UpdateAllTimer();
        }
    }

}
