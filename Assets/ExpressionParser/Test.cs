using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
		if(Input.GetKeyDown(KeyCode.Space))
		{
			//  enhanceLevel<=10?10:enhanceLevel
			string script = "enhanceLevel<=10?10:enhanceLevel";
			Dictionary<string, int> vul = new Dictionary<string, int>();
			vul.Add("enhanceLevel", 8);
			int num = CalculateArenaUtils.CalculateArenaRewardProp(vul, script);
			Debug.LogError(num);
		}
	}
}
