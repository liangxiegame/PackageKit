using System;
using System.Collections.Generic;

namespace QFramework
{
    public interface IPackageKitView
    {
        PackageKitContainer Container { get; set; }

        void Init(PackageKitContainer container);

        void OnUpdate();
        void OnGUI();

        void OnDispose();
        void OnShow();
        void OnHide();
    }
    
    public class PackageKitContainer
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
}