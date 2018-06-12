﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MMONET
{
    /// <summary>
    /// 更新委托
    /// </summary>
    /// <param name="delta"></param>
    public delegate void Update(double delta);
    /// <summary>
    /// 主线程调度器
    /// <para>Unity中请使用Unity的主线程轮询</para>
    /// </summary>
    public class MainThreadScheduler
    {
        private MainThreadScheduler() { }

        static readonly List<Update> updates = new List<Update>();

        static readonly List<(bool addOrRemove, Update Update)> ar = new List<(bool addOrRemove, Update Update)>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="update"></param>
        public static void Add(Update update)
        {
            lock (ar)
            {
                ar.Add((true, update));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="update"></param>
        public static void Remove(Update update)
        {
            lock (ar)
            {
                ar.Add((false, update));
            }
        }

        /// <summary>
        /// 在主线程调用，传入距离上次调用的时间。
        /// 可以理解为所有异步方法的后续部分都在这个方法中执行。
        /// </summary>
        /// <param name="delta"></param>
        public static void Update(double delta)
        {
            
            lock (ar)
            {
                foreach (var item in ar)
                {
                    if (item.addOrRemove)
                    {
                        updates.Add(item.Update);
                    }
                    else
                    {
                        updates.Remove(item.Update);
                    }
                }

                ar.Clear();
            }


            foreach (var item in updates)
            {
                item?.Invoke(delta);
            }
        }
    }
}