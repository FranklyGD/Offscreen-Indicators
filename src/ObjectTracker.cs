using System;
using System.Collections.Generic;

namespace OffscreenIndicators {
	class ObjectTracker<T,V> {
		public Func<IEnumerable<T>> OnGetCollection;
		public Func<T, V> OnCreate;
		public Action<V> OnDestroy;
		public Func<T, bool> OnExistCheck;
		Dictionary<T, V> trackedObjects = new();

		public int Count {
			get {
				return trackedObjects.Count;
			}
		}

		public void Update() {
			if (trackedObjects == null) {
				return;
			}
			
			List<T> unprocessedObjects = new(OnGetCollection());
			List<T> removedObjects = new();

			unprocessedObjects.RemoveAll(x => !OnExistCheck(x));

			foreach (var pair in trackedObjects) {
				if (!unprocessedObjects.Exists(x => x.Equals(pair.Key))) {
					removedObjects.Add(pair.Key);
				}
			}

			foreach (T removedObject in removedObjects) {
				if (OnDestroy != null) {
					this.OnDestroy(trackedObjects[removedObject]);
				}
				trackedObjects.Remove(removedObject);
			}

			foreach (T unprocessedObject in unprocessedObjects) {
				V trackedObject;
				if (!trackedObjects.TryGetValue(unprocessedObject, out trackedObject)) {
					trackedObject = OnCreate(unprocessedObject);
					trackedObjects[unprocessedObject] = trackedObject;
				}
			}
		}

		public IEnumerable<V> GetTrackers() {
			return trackedObjects.Values;
		}

		public void Cleanup() {
			if (OnDestroy != null) {
				foreach (V removedObject in GetTrackers()) {
					OnDestroy(removedObject);
				}
			}
			trackedObjects = null;
		}
	}
}