﻿using System.Collections;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices {
    [CLSCompliant(false)]
    public delegate void NativeEventHandler(string providerName, uint controllerIndex, ulong data0, ulong data1, IntPtr data2, DateTime timestamp);

    public sealed class NativeEventDispatcher : IDisposable {
        private static Hashtable instances = new Hashtable();

        private NativeEventHandler m_threadSpawn = null;
        private NativeEventHandler m_callbacks = null;
        private bool m_disposed = false;
        private object m_NativeEventDispatcher;
        private string name;

        [MethodImpl(MethodImplOptions.InternalCall)]
        extern private NativeEventDispatcher(string name);

        [MethodImpl(MethodImplOptions.InternalCall)]
        extern public void EnableInterrupt();

        [MethodImpl(MethodImplOptions.InternalCall)]
        extern public void DisableInterrupt();

        [MethodImpl(MethodImplOptions.InternalCall)]
        extern private void Dispose(bool disposing);

        ~NativeEventDispatcher() {
            Dispose(false);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Dispose() {
            if (!this.m_disposed) {
                NativeEventDispatcher.instances.Remove(this.name);

                Dispose(true);

                GC.SuppressFinalize(this);

                this.m_disposed = true;
            }
        }

        public static NativeEventDispatcher GetDispatcher(string name) {
            if (name == null) throw new ArgumentNullException(nameof(name));

            if (NativeEventDispatcher.instances.Contains(name))
                return (NativeEventDispatcher)NativeEventDispatcher.instances[name];

            var inst = new NativeEventDispatcher(name) { name = name };

            NativeEventDispatcher.instances[name] = inst;

            return inst;
        }

        [CLSCompliant(false)]
        public event NativeEventHandler OnInterrupt {
            [MethodImpl(MethodImplOptions.Synchronized)]
            add {
                if (this.m_disposed) {
                    throw new ObjectDisposedException();
                }

                NativeEventHandler callbacksOld = this.m_callbacks;
                NativeEventHandler callbacksNew = (NativeEventHandler)Delegate.Combine(callbacksOld, value);

                try {
                    this.m_callbacks = callbacksNew;

                    if (callbacksNew != null) {
                        if (callbacksOld == null) {
                            EnableInterrupt();
                        }

                        if (callbacksNew.Equals(value) == false) {
                            callbacksNew = new NativeEventHandler(this.MultiCastCase);
                        }
                    }

                    this.m_threadSpawn = callbacksNew;
                }
                catch {
                    this.m_callbacks = callbacksOld;

                    if (callbacksOld == null) {
                        DisableInterrupt();
                    }

                    throw;
                }
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            remove {
                if (this.m_disposed) {
                    throw new ObjectDisposedException();
                }

                NativeEventHandler callbacksOld = this.m_callbacks;
                NativeEventHandler callbacksNew = (NativeEventHandler)Delegate.Remove(callbacksOld, value);

                try {
                    this.m_callbacks = (NativeEventHandler)callbacksNew;

                    if (callbacksNew == null && callbacksOld != null) {
                        DisableInterrupt();
                    }
                }
                catch {
                    this.m_callbacks = callbacksOld;

                    throw;
                }
            }
        }

        private void MultiCastCase(string providerName, uint controllerIndex, ulong data0, ulong data1, IntPtr data2, DateTime timestamp) => this.m_callbacks?.Invoke(providerName, controllerIndex, data0, data1, data2, timestamp);
    }
}
