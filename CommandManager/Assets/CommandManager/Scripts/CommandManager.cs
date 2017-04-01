using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace GM
{
	[Serializable]
	public class Command
	{
		[XmlAttribute]
		public string name;
		[XmlAttribute]
		public string desc;
		[XmlAttribute]
		public string defaultArg;
	}

	[Serializable]
	[XmlRootAttribute ("Commands")]
	public class CommandArray
	{
		[XmlArrayAttribute ("Items")]
		public Command[] items = new Command[0];
	}

	public class CommandManager : MonoBehaviour
	{
		const string assetsRuntimeConsoleResourcescmdxml = "Assets/CommandManager/Resources/Commands.xml";
	
		[SerializeField]
		CommandArray commands = null;

		public event Action<Command, string[]> onSubmit;

		Vector2 scrollPosition = Vector2.zero;
		Vector2 scrollPositionCMD = Vector2.zero;
	
		bool messageUpdated = false;
	
		ArrayList messages = new ArrayList ();
		string inputString = string.Empty;
		bool cmdOn = false;
		bool traceOn = false;

		[ContextMenu ("SaveCommands")]
		void SaveCommands ()
		{ 
			XmlSerializer serializer = new XmlSerializer (typeof(CommandArray));
			TextWriter writer = new StreamWriter (assetsRuntimeConsoleResourcescmdxml);
			serializer.Serialize (writer, commands);
		}

		void LoadCommands ()
		{
			XmlSerializer serializer = new XmlSerializer (typeof(CommandArray));
			TextReader reader = new StreamReader (assetsRuntimeConsoleResourcescmdxml);
			commands = serializer.Deserialize (reader) as CommandArray;
		}

		void Start ()
		{ 
			Application.logMessageReceived += Application_logMessageReceived;    
			LoadCommands ();
		}

		void Application_logMessageReceived (string condition, string stackTrace, LogType type)
		{
			string[] colorNames = { "red", "red", "yellow", "white", "red" }; 
			var msg = string.Format ("<color={0}>{1}{2}</color>", colorNames [(int)type], condition, stackTrace); 
			messages.Add (msg); 
			messageUpdated = true;
		}

		void Submit ()
		{ 
			Debug.Log (string.Format ("[CommandManager] Submit: ", inputString));  
			if (inputString.StartsWith ("@")) {
				if (commands != null && commands.items != null) {
					string[] fields = inputString.Split (new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
					string cmdName = fields [0].TrimStart ('@');
					var cmd = Array.Find (commands.items, delegate(Command obj) {
						return obj.name == cmdName;
					}); 
					int count = fields.Length - 1;
					string[] args = new string[count];
					if (count > 0) { 
						Array.ConstrainedCopy (fields, 1, args, 0, count); 
					}
					if (cmd != null) {
						if (onSubmit != null) {
							onSubmit.Invoke (cmd, args);
						} 
					}
				}
			}
		}

		bool TryUseReturnOrEnter ()
		{
			if (Event.current.isKey && (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return)) {
				Event.current.Use ();
				return true;
			} 
			return false;
		}

	
		void OnGUI ()
		{  
			using (var v = new GUILayout.VerticalScope (GUILayout.Width (Screen.width), GUILayout.Height (Screen.height))) {
				GUILayout.FlexibleSpace (); 
				if (traceOn) {   
					if (messages.Count > 0) {
						using (var v1 = new GUILayout.VerticalScope ("box")) { 
							using (var s = new GUILayout.ScrollViewScope (scrollPosition)) {
								float height = 0;
								for (int i = 0; i < messages.Count; i++) {
									var msg = messages [i] as string;
									GUIContent c = new GUIContent (msg); 
									GUILayout.Label (c);
									height += GUI.skin.label.CalcSize (c).y;
								}
								scrollPosition = s.scrollPosition;
								if (messageUpdated) {
									scrollPosition.y = height;
									messageUpdated = false;
								} 
							}
						}
					}  
				}  
				if (cmdOn) {
					using (var v1 = new GUILayout.VerticalScope ("Commands", "window")) {
						using (var s = new GUILayout.ScrollViewScope (scrollPositionCMD)) { 
							var style = new GUIStyle(GUI.skin.button);
							style.alignment = TextAnchor.MiddleLeft;
							for (int i = 0; i < commands.items.Length; i++) {
								var item = commands.items [i];
								var itemName = string.Format ("{0}. {1} ({2} 默认参数 {3})", i, item.name, item.desc, item.defaultArg);
								if (GUILayout.Button (itemName, style)) {
									inputString = string.Format ("@{0} {1}", item.name, item.defaultArg);
								}
							}
							scrollPositionCMD = s.scrollPosition; 
						} 
					}
				}
				using (var h = new GUILayout.HorizontalScope ()) {  
					traceOn = GUILayout.Toggle (traceOn, "Trace", "button", GUILayout.Width (64)); 
					cmdOn = GUILayout.Toggle (cmdOn, "CMD", "button", GUILayout.Width (64));   
					inputString = GUILayout.TextField (inputString);  
					if (GUILayout.Button ("Enter", GUILayout.Width (64)) || TryUseReturnOrEnter ()) {
						if (!string.IsNullOrEmpty (inputString)) {
							Submit ();
						}
					}
				}
				GUILayout.Space (6);
			}
		}
	}
}
