using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text;

public class BallLouncher : MonoBehaviour
{
    public Rigidbody ball;
    public Transform target;
    public float h = 25;
    public float gravity = 18;
    public bool debugPath;
    public TextMeshProUGUI info;
    public TextMeshProUGUI info2;
    StringBuilder m_StringBuilder = new StringBuilder(0, 300);
    StringBuilder m_StringBuilder_info2 = new StringBuilder(0, 300);
    float time = 0;
    float velocidad = 0;

    void Start(){
        ball.useGravity = false;
        gravity = -gravity;
    }

    void Update(){
        if(Input.GetKeyDown(KeyCode.Space) || Input.touchCount > 0){
            Launch();
        }
        if (debugPath) {
			DrawPath ();
		}

        m_StringBuilder.Clear();
        m_StringBuilder.Append("Altura maxima: ");
        m_StringBuilder.Append(h.ToString(".##"));
        m_StringBuilder.Append(" m \n");

        m_StringBuilder.Append("Gravedad: ");
        m_StringBuilder.Append((-gravity).ToString(".##"));
        m_StringBuilder.Append(" m/s^2 \n");

        m_StringBuilder_info2.Clear();
        m_StringBuilder_info2.Append("Tiempo: ");
        if (time == 0)
            m_StringBuilder_info2.Append(" ?");
        else
            m_StringBuilder_info2.Append(time.ToString(".##"));
        m_StringBuilder_info2.Append(" segundos \n");

        m_StringBuilder_info2.Append("Velocidad inicial: ");
        if (velocidad == 0)
            m_StringBuilder_info2.Append(" ?");
        else
            m_StringBuilder_info2.Append(velocidad.ToString(".##"));
        m_StringBuilder_info2.Append(" m/s \n");

        info.text = m_StringBuilder.ToString();
        info2.text = m_StringBuilder_info2.ToString();
    }

    void Launch(){
        Physics.gravity = Vector3.up*gravity;
        ball.useGravity = true;
        ball.velocity = CalculateLaunchData().initialVelocity;
        time = CalculateLaunchData().timeToTarget;
        velocidad = ball.velocity.magnitude;
    }

    LaunchData CalculateLaunchData(){
        float displacementY = target.position.y - ball.position.y;
        Vector3 displacementXZ = new Vector3(target.position.x - ball.position.x, 0, target.position.z - ball.position.z);
        float time = Mathf.Sqrt(-2*h/gravity) + Mathf.Sqrt(2*(displacementY-h)/gravity);
        Vector3 velocityY = Vector3.up*Mathf.Sqrt(-2*gravity*h);
        Vector3 velocityXZ = displacementXZ/(Mathf.Sqrt(-2*h/gravity) + Mathf.Sqrt(2*(displacementY-h)/gravity));
        
        return new LaunchData(velocityXZ + velocityY * -Mathf.Sign(gravity), time);
    }

    void DrawPath() {
		LaunchData launchData = CalculateLaunchData ();
		Vector3 previousDrawPoint = ball.position;

		int resolution = 30;
		for (int i = 1; i <= resolution; i++) {
			float simulationTime = i / (float)resolution * launchData.timeToTarget;
			Vector3 displacement = launchData.initialVelocity * simulationTime + Vector3.up *gravity * simulationTime * simulationTime / 2f;
			Vector3 drawPoint = ball.position + displacement;
			Debug.DrawLine (previousDrawPoint, drawPoint, Color.green);
			previousDrawPoint = drawPoint;
		}
	}

    struct LaunchData {
		public readonly Vector3 initialVelocity;
		public readonly float timeToTarget;

		public LaunchData (Vector3 initialVelocity, float timeToTarget)
		{
			this.initialVelocity = initialVelocity;
			this.timeToTarget = timeToTarget;
		}
		
	}
}
