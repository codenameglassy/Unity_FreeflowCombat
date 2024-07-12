using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonAnimation : MonoBehaviour {
	float initial_size_x;
	float initial_size_y;
	public float factor = 0.5f;
	public float speed = 1.15f;
	bool GO = false;

	void Awake () {
		initial_size_x = transform.localScale.x;
		initial_size_y = transform.localScale.y;
	}
	
	void FixedUpdate () {
		if(GO)
		{
			float aux_x = transform.localScale.x;
			float aux_y = transform.localScale.y;
			if(transform.localScale.y < initial_size_y)
			{
				aux_x *= speed;
				aux_y *= speed;
				transform.localScale = new Vector3 (aux_x, aux_y, 1.0f);
			}
			else
			{
				aux_x = initial_size_x;
				aux_y = initial_size_y;
				transform.localScale = new Vector3 (aux_x, aux_y, 1.0f);
				GO = false;
			}

		}
	}

	void Go()
	{
		GO = true;
		transform.localScale = new Vector3 (initial_size_x*factor, initial_size_y*factor, 1.0f);
	}
}
