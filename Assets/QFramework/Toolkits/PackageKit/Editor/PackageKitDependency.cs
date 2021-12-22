/****************************************************************************
 * Copyright (c) 2015 ~ 2022 liangxiegame MIT License
 *
 * QFramework v1.0
 *
 * https://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * 
 * Author:
 *  liangxie        https://github.com/liangxie
 *  soso            https://github.com/so-sos-so
 *
 * Contributor
 *  TastSong        https://github.com/TastSong
 * 
 * Community
 *  QQ Group: 623597263
 ****************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace QFramework
{
    #region Architecture
    internal interface IArchitecture
    {
        void RegisterSystem<T>(T system) where T : ISystem;

        void RegisterModel<T>(T model) where T : IModel;

        void RegisterUtility<T>(T utility) where T : IUtility;

        T GetSystem<T>() where T : class, ISystem;

        T GetModel<T>() where T : class, IModel;

        T GetUtility<T>() where T : class, IUtility;

        void SendCommand<T>() where T : ICommand, new();
        void SendCommand<T>(T command) where T : ICommand;

        TResult SendQuery<TResult>(IQuery<TResult> query);
        
        void SendEvent<T>() where T : new();
        void SendEvent<T>(T e);

        IUnRegister RegisterEvent<T>(Action<T> onEvent);
        void UnRegisterEvent<T>(Action<T> onEvent);
    }

    internal abstract class Architecture<T> : IArchitecture where T : Architecture<T>, new()
    {
        /// <summary>
        /// 是否初始化完成 
        /// </summary>
        private bool mInited = false;

        private List<ISystem> mSystems = new List<ISystem>();

        private List<IModel> mModels = new List<IModel>();

        public static Action<T> OnRegisterPatch = architecture => { };

        private static T mArchitecture;

        public static IArchitecture Interface
        {
            get
            {
                if (mArchitecture == null)
                {
                    MakeSureArchitecture();
                }

                return mArchitecture;
            }
        }


        static void MakeSureArchitecture()
        {
            if (mArchitecture == null)
            {
                mArchitecture = new T();
                mArchitecture.Init();

                OnRegisterPatch?.Invoke(mArchitecture);

                foreach (var architectureModel in mArchitecture.mModels)
                {
                    architectureModel.Init();
                }

                mArchitecture.mModels.Clear();

                foreach (var architectureSystem in mArchitecture.mSystems)
                {
                    architectureSystem.Init();
                }

                mArchitecture.mSystems.Clear();

                mArchitecture.mInited = true;
            }
        }

        protected abstract void Init();

        private IOCContainer mContainer = new IOCContainer();

        public void RegisterSystem<TSystem>(TSystem system) where TSystem : ISystem
        {
            system.SetArchitecture(this);
            mContainer.Register<TSystem>(system);

            if (!mInited)
            {
                mSystems.Add(system);
            }
            else
            {
                system.Init();
            }
        }

        public void RegisterModel<TModel>(TModel model) where TModel : IModel
        {
            model.SetArchitecture(this);
            mContainer.Register<TModel>(model);

            if (!mInited)
            {
                mModels.Add(model);
            }
            else
            {
                model.Init();
            }
        }

        public void RegisterUtility<TUtility>(TUtility utility) where TUtility : IUtility
        {
            mContainer.Register<TUtility>(utility);
        }

        public TSystem GetSystem<TSystem>() where TSystem : class, ISystem
        {
            return mContainer.Get<TSystem>();
        }

        public TModel GetModel<TModel>() where TModel : class, IModel
        {
            return mContainer.Get<TModel>();
        }

        public TUtility GetUtility<TUtility>() where TUtility : class, IUtility
        {
            return mContainer.Get<TUtility>();
        }

        public void SendCommand<TCommand>() where TCommand : ICommand, new()
        {
            var command = new TCommand();
            command.SetArchitecture(this);
            command.Execute();
        }

        public void SendCommand<TCommand>(TCommand command) where TCommand : ICommand
        {
            command.SetArchitecture(this);
            command.Execute();
        }

        public TResult SendQuery<TResult>(IQuery<TResult> query)
        {
            query.SetArchitecture(this);
            return query.Do();
        }

        private ITypeEventSystem mTypeEventSystem = new TypeEventSystem();
        
        public void SendEvent<TEvent>() where TEvent : new()
        {
            mTypeEventSystem.Send<TEvent>();            
        }

        public void SendEvent<TEvent>(TEvent e)
        {
            mTypeEventSystem.Send<TEvent>(e);            
        }

        public IUnRegister RegisterEvent<TEvent>(Action<TEvent> onEvent)
        {
            return mTypeEventSystem.Register<TEvent>(onEvent);
        }

        public void UnRegisterEvent<TEvent>(Action<TEvent> onEvent)
        {
            mTypeEventSystem.UnRegister<TEvent>(onEvent);
        }
    }
    
    internal interface IOnEvent<T>
    {
        void OnEvent(T e);
    }

    internal static class OnGlobalEventExtension
    {
        public static IUnRegister RegisterEvent<T>(this IOnEvent<T> self) where T : struct
        {
            return TypeEventSystem.Global.Register<T>(self.OnEvent);
        }

        public static void UnRegisterEvent<T>(this IOnEvent<T> self) where T : struct
        {
            TypeEventSystem.Global.UnRegister<T>(self.OnEvent);
        }
    }
    #endregion

    #region Controller

    internal interface IController : IBelongToArchitecture,ICanSendCommand,ICanGetSystem,ICanGetModel,ICanRegisterEvent,ICanSendQuery
    {

    }
    #endregion

    #region System
    internal interface ISystem : IBelongToArchitecture ,ICanSetArchitecture,ICanGetModel,ICanGetUtility,ICanRegisterEvent,ICanSendEvent,ICanGetSystem
    {
        void Init();
    }

    internal abstract class AbstractSystem : ISystem
    {
        private IArchitecture mArchitecture;
        IArchitecture IBelongToArchitecture.GetArchitecture()
        {
            return mArchitecture;
        }

        void ICanSetArchitecture.SetArchitecture(IArchitecture architecture)
        {
            mArchitecture = architecture;
        }

        void ISystem.Init()
        {
            OnInit();
        }

        protected abstract void OnInit();
    }
    

    #endregion

    #region Model
    internal interface IModel : IBelongToArchitecture,ICanSetArchitecture,ICanGetUtility,ICanSendEvent
    {
        void Init();
    }

    internal abstract class AbstractModel: IModel
    {
        private IArchitecture mArchitecturel;
        
        IArchitecture IBelongToArchitecture.GetArchitecture()
        {
            return mArchitecturel;
        }

        void ICanSetArchitecture.SetArchitecture(IArchitecture architecture)
        {
            mArchitecturel = architecture;
        }

        void IModel.Init()
        {
            OnInit();
        }

        protected abstract void OnInit();
    }
    

    #endregion

    #region Utility
    internal interface IUtility
    {
    }
    #endregion

    #region Command
    internal interface ICommand : IBelongToArchitecture,ICanSetArchitecture,ICanGetSystem,ICanGetModel,ICanGetUtility,ICanSendEvent,ICanSendCommand,ICanSendQuery
    {
        void Execute();
    }

    internal abstract class AbstractCommand : ICommand
    {
        private IArchitecture mArchitecture;
        IArchitecture IBelongToArchitecture.GetArchitecture()
        {
            return mArchitecture;
        }

        void ICanSetArchitecture.SetArchitecture(IArchitecture architecture)
        {
            mArchitecture = architecture;
        }

        void ICommand.Execute()
        {
            OnExecute();
        }

        protected abstract void OnExecute();
    }
    

    #endregion
    
    #region Query

    internal interface IQuery<TResult> : IBelongToArchitecture,ICanSetArchitecture,ICanGetModel,ICanGetSystem,ICanSendQuery
    {
        TResult Do();
    }
    
    internal abstract class AbstractQuery<T> : IQuery<T>
    {
        public T Do()
        {
            return OnDo();
        }

        protected abstract T OnDo();


        private IArchitecture mArchitecture;
        
        public IArchitecture GetArchitecture()
        {
            return mArchitecture;
        }

        public void SetArchitecture(IArchitecture architecture)
        {
            mArchitecture = architecture;
        }
    }

    #endregion

    #region Rule
    internal interface IBelongToArchitecture
    {
        IArchitecture GetArchitecture();
    }
    
    internal interface ICanSetArchitecture
    {
        void SetArchitecture(IArchitecture architecture);
    }
    
    internal interface ICanGetModel : IBelongToArchitecture
    {
    }
    
    internal static class CanGetModelExtension
    {
        public static T GetModel<T>(this ICanGetModel self) where T : class, IModel
        {
            return self.GetArchitecture().GetModel<T>();
        }
    }
    
    internal interface ICanGetSystem : IBelongToArchitecture
    {
        
    }
    
    internal static class CanGetSystemExtension
    {
        public static T GetSystem<T>(this ICanGetSystem self) where T : class, ISystem
        {
            return self.GetArchitecture().GetSystem<T>();
        }
    }
    
    internal interface ICanGetUtility : IBelongToArchitecture
    {

    }

    internal static class CanGetUtilityExtension
    {
        public static T GetUtility<T>(this ICanGetUtility self) where T : class, IUtility
        {
            return self.GetArchitecture().GetUtility<T>();
        }
    }
    
    internal interface ICanRegisterEvent : IBelongToArchitecture
    {
    }

    internal static class CanRegisterEventExtension
    {
        public static IUnRegister RegisterEvent<T>(this ICanRegisterEvent self, Action<T> onEvent)
        {
            return self.GetArchitecture().RegisterEvent<T>(onEvent);
        }
        
        public static void UnRegisterEvent<T>(this ICanRegisterEvent self, Action<T> onEvent)
        {
            self.GetArchitecture().UnRegisterEvent<T>(onEvent);
        }
    }
    
    internal interface ICanSendCommand : IBelongToArchitecture
    {

    }

    internal static class CanSendCommandExtension
    {
        public static void SendCommand<T>(this ICanSendCommand self) where T : ICommand, new()
        {
            self.GetArchitecture().SendCommand<T>();
        }
        
        public static void SendCommand<T>(this ICanSendCommand self,T command) where T : ICommand
        {
            self.GetArchitecture().SendCommand<T>(command);
        }
    }
    
    internal interface ICanSendEvent : IBelongToArchitecture
    {
    }

    internal static class CanSendEventExtension
    {
        public static void SendEvent<T>(this ICanSendEvent self) where T : new()
        {
            self.GetArchitecture().SendEvent<T>();
        }

        public static void SendEvent<T>(this ICanSendEvent self, T e)
        {
            self.GetArchitecture().SendEvent<T>(e);
        }
    }
    
    internal interface ICanSendQuery : IBelongToArchitecture
    {
        
    }

    internal static class CanSendQueryExtension
    {
        public static TResult SendQuery<TResult>(this ICanSendQuery self, IQuery<TResult> query)
        {
            return self.GetArchitecture().SendQuery(query);
        }
    }
    #endregion

    #region TypeEventSystem

    internal interface ITypeEventSystem
    {
        void Send<T>() where T : new();
        void Send<T>(T e);
        IUnRegister Register<T>(Action<T> onEvent);
        void UnRegister<T>(Action<T> onEvent);
    }

    internal interface IUnRegister
    {
        void UnRegister();
    }

    internal interface IUnRegisterList
    {
        List<IUnRegister> UnregisterList { get; }
    }

    internal static class IUnRegisterListExtension
    {
        public static void AddToUnregisterList(this IUnRegister self, IUnRegisterList unRegisterList)
        {
            unRegisterList.UnregisterList.Add(self);
        }

        public static void UnRegisterAll(this IUnRegisterList self)
        {
            foreach (var unRegister in self.UnregisterList)
            {
                unRegister.UnRegister();
            }
            
            self.UnregisterList.Clear();
        }
    }

    internal struct TypeEventSystemUnRegister<T> : IUnRegister
    {
        public ITypeEventSystem TypeEventSystem;
        public Action<T> OnEvent;
        
        public void UnRegister()
        {
            TypeEventSystem.UnRegister<T>(OnEvent);

            TypeEventSystem = null;

            OnEvent = null;
        }
    }
    
    /// <summary>
    /// 自定义可注销的类
    /// </summary>
    internal class CustomUnRegister : IUnRegister
    {
        /// <summary>
        /// 委托对象
        /// </summary>
        private Action mOnUnRegsiter = null;

        /// <summary>
        /// 带参构造函数
        /// </summary>
        /// <param name="onDispose"></param>
        public CustomUnRegister(Action onUnRegsiter)
        {
            mOnUnRegsiter = onUnRegsiter;
        }

        /// <summary>
        /// 资源释放
        /// </summary>
        public void UnRegister()
        {
            mOnUnRegsiter.Invoke();
            mOnUnRegsiter = null;
        }
    }

    internal class UnRegisterOnDestroyTrigger : MonoBehaviour
    {
        private HashSet<IUnRegister> mUnRegisters = new HashSet<IUnRegister>();

        public void AddUnRegister(IUnRegister unRegister)
        {
            mUnRegisters.Add(unRegister);
        }

        private void OnDestroy()
        {
            foreach (var unRegister in mUnRegisters)
            {
                unRegister.UnRegister();
            }
            
            mUnRegisters.Clear();
        }
    }

    internal static class UnRegisterExtension
    {
        public static void UnRegisterWhenGameObjectDestroyed(this IUnRegister unRegister, GameObject gameObject)
        {
            var trigger = gameObject.GetComponent<UnRegisterOnDestroyTrigger>();

            if (!trigger)
            {
                trigger = gameObject.AddComponent<UnRegisterOnDestroyTrigger>();
            }

            trigger.AddUnRegister(unRegister);
        }
    }
    
    internal class TypeEventSystem  : ITypeEventSystem
    {
        public interface IRegistrations
        {
            
        }
        
        public class Registrations<T> : IRegistrations
        {
            public Action<T> OnEvent = e => { };
        }

        private Dictionary<Type, IRegistrations> mEventRegistration = new Dictionary<Type, IRegistrations>();


        public static readonly TypeEventSystem Global = new TypeEventSystem();
        
        public void Send<T>() where T : new()
        {
            var e = new T();
            Send<T>(e);
        }

        public void Send<T>(T e)
        {
            var type = typeof(T);
            IRegistrations registrations;

            if (mEventRegistration.TryGetValue(type, out registrations))
            {
                (registrations as Registrations<T>).OnEvent(e);
            }
        }

        public IUnRegister Register<T>(Action<T> onEvent)
        {
            var type = typeof(T);
            IRegistrations registrations;

            if (mEventRegistration.TryGetValue(type, out registrations))
            {
                
            }
            else
            {
                registrations = new Registrations<T>();
                mEventRegistration.Add(type, registrations);
            }

            (registrations as Registrations<T>).OnEvent += onEvent;

            return new TypeEventSystemUnRegister<T>()
            {
                OnEvent = onEvent,
                TypeEventSystem = this
            };
        }

        public void UnRegister<T>(Action<T> onEvent)
        {
            var type = typeof(T);
            IRegistrations registrations;

            if (mEventRegistration.TryGetValue(type, out registrations))
            {
                (registrations as Registrations<T>).OnEvent -= onEvent;
            }
        }
    }

    #endregion

    #region IOC
    internal class IOCContainer
    {
        private Dictionary<Type, object> mInstances = new Dictionary<Type, object>();

        public void Register<T>(T instance)
        {
            var key = typeof(T);

            if (mInstances.ContainsKey(key))
            {
                mInstances[key] = instance;
            }
            else
            {
                mInstances.Add(key, instance);
            }
        }

        public T Get<T>() where T : class
        {
            var key = typeof(T);

            if (mInstances.TryGetValue(key, out var retInstance))
            {
                return retInstance as T;
            }

            return null;
        }
    }
    

    #endregion

    #region BindableProperty
    internal class BindableProperty<T>
    {

        public BindableProperty(T defaultValue = default)
        {
            mValue = defaultValue;
        }

        protected T mValue = default(T);

        public T Value
        {
            get => mValue;
            set
            {
                if (value == null && mValue == null) return;
                if (value != null && value.Equals(mValue)) return;
                
                mValue = value;
                mOnValueChanged?.Invoke(value);
            }
        }
        
        private Action<T> mOnValueChanged = (v) => { }; 

        public IUnRegister Register(Action<T> onValueChanged) 
        {
            mOnValueChanged += onValueChanged;
            return new BindablePropertyUnRegister<T>()
            {
                BindableProperty = this,
                OnValueChanged = onValueChanged
            };
        }
        
        public IUnRegister RegisterWithInitValue(Action<T> onValueChanged)
        {
            onValueChanged(mValue);
            return Register(onValueChanged);
        }

        public static implicit operator T(BindableProperty<T> property)
        {
            return property.Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public void UnRegister(Action<T> onValueChanged)
        {
            mOnValueChanged -= onValueChanged;
        }

    }
    
    internal class BindablePropertyUnRegister<T> : IUnRegister
    {
        
        public BindableProperty<T> BindableProperty { get; set; }
      
        public Action<T> OnValueChanged { get; set; }
      
        public void UnRegister()
        {
            BindableProperty.UnRegister(OnValueChanged);

            BindableProperty = null;
            OnValueChanged = null;
        }
    }
    
    #endregion
}