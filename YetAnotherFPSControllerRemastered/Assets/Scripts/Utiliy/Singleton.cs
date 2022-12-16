/** 
 * Original code by Pierluca Lanzi (Politecnico di Milano)
 */


using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Component {
	// --- Private members
	private static T s_instance;

	// --- Public properties
	public static T instance {
		get {
			if (!s_instance) {
				s_instance = FindObjectOfType<T>();
				if (!s_instance) {
					GameObject obj = new GameObject();
					obj.name = typeof(T).Name;
					s_instance = obj.AddComponent<T>();
					DontDestroyOnLoad(obj);
				}
				else DontDestroyOnLoad(s_instance.gameObject);
				//Debug.Log("Instantiation of Singleton of type " + typeof(T).Name + " in Singleton.instance");
			}
			return s_instance;
		}

		private set { s_instance = value; }
	}


	// --- Singleton methods
	protected bool Instantiate()
	{
		//Debug.Log("Singleton.Instantiate() run by " + gameObject.name.ToString());
		if (s_instance == this)
			return true;
		else if (s_instance == null) {
			s_instance = this as T;
			DontDestroyOnLoad(gameObject);
			return true;
		} else {
			Destroy(gameObject);
			return false;
		}
	}
}