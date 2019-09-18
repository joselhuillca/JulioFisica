using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KartGame.KartSystems;
using UnityEngine;
using TMPro;
using System.Text;
using UnityEngine.UI;

namespace KartGame.Track
{
    /// <summary>
    /// A MonoBehaviour to deal with all the time and positions for the racers.
    /// </summary>
    public class TrackManager : MonoBehaviour
    {
        [Tooltip ("A reference to a script that provides information about the kart's movement, usually the KartMovmeent script.  This must implement IKartInfo.")]
        [RequireInterface (typeof(IKartInfo))]
        public Object kartMovement;
        IKartInfo m_KartMovement;
        //[Tooltip ("Texto para mostrar la velocidad y aceleracion")]
        public TextMeshProUGUI infoVelocity;
        StringBuilder m_StringBuilder = new StringBuilder(0, 300);
        [RequireInterface (typeof(IInput))]
        [Tooltip ("A reference to the an object implementing the IInput class to be used to control the kart.")]
        public Object input;
        IInput m_Input;

        [Tooltip ("The name of the track in this scene.  Used for track time records.  Must be unique.")]
        public string trackName;
        [Tooltip ("Number of laps for the race.")]
        public int raceLapTotal;
        [Tooltip ("All the checkpoints for the track in the order that they should be completed starting with the start/finish line checkpoint.")]
        public List<Checkpoint> checkpoints = new List<Checkpoint> ();
        [Tooltip("Reference to an object responsible for repositioning karts.")]
        public KartRepositioner kartRepositioner;

        public GameObject popUpPreguntas;
        public int tema = 1;
        // Deberia de haber una base de preguntas:
        string[] questions_mru = {"Cual afirmacion es correcta:", 
                                "Cuando en un movimiento la velocidad no varía es:"};
        string[,] answersOptions_mru = {{"velocidad=tiempo/distancia", "tiempo=velocidad/distacia", "distancia=velocidad*tiempo"},
                                        {"Rectilíneo.", "Uniforme.", "Decelerado."}};
        int[] answers_mru = {2, 1};

        string[] questions_mruv = {"Cuando a velocidad y la aceleración tienen el mismo sentido, el movimiento:", 
                                "En el movimiento Rectílineo Uniformemente Variado, la aceleración:"};
        string[,] answersOptions_mruv = {{"no tiene aceleracion", "es acelerado", "es desacelerado"},
                                        {"aumenta con el tiempo", "es constante", "disminuye con el tiempo"}};
        int[] answers_mruv = {1, 1};
        public TextMeshProUGUI preguntaPopUp;
        public Text message;
        public List<Toggle> listapreguntas = new List<Toggle> ();
        int currentLapAnswer;

        float acelerar = 1f;
        public GameObject panelEndGame;

        bool m_IsRaceRunning;
        Dictionary<IRacer, Checkpoint> m_RacerNextCheckpoints = new Dictionary<IRacer, Checkpoint> (16);
        TrackRecord m_SessionBestLap = TrackRecord.CreateDefault ();
        TrackRecord m_SessionBestRace = TrackRecord.CreateDefault ();
        TrackRecord m_HistoricalBestLap;
        TrackRecord m_HistoricalBestRace;

        KartStats m_ModifiedStats; // The stats that are used to calculate the kart's velocity.

        public bool IsRaceRunning => m_IsRaceRunning;

        /// <summary>
        /// Returns the best lap time recorded this session.  If no record is found, -1 is returned.
        /// </summary>
        public float SessionBestLap
        {
            get
            {
                if (m_SessionBestLap != null && m_SessionBestLap.time < float.PositiveInfinity)
                    return m_SessionBestLap.time;
                return -1f;
            }
        }

        /// <summary>
        /// Returns the best race time recorded this session.  If no record is found, -1 is returned.
        /// </summary>
        public float SessionBestRace
        {
            get
            {
                if (m_SessionBestRace != null && m_SessionBestRace.time < float.PositiveInfinity)
                    return m_SessionBestRace.time;
                return -1f;
            }
        }

        /// <summary>
        /// Returns the best lap time ever recorded.  If no record is found, -1 is returned.
        /// </summary>
        public float HistoricalBestLap
        {
            get
            {
                if (m_HistoricalBestLap != null && m_HistoricalBestLap.time < float.PositiveInfinity)
                    return m_HistoricalBestLap.time;
                return -1f;
            }
        }

        /// <summary>
        /// Returns the best race time ever recorded.  If no record is found, -1 is returned.
        /// </summary>
        public float HistoricalBestRace
        {
            get
            {
                if (m_HistoricalBestRace != null && m_HistoricalBestRace.time < float.PositiveInfinity)
                    return m_HistoricalBestRace.time;
                return -1f;
            }
        }

        void Awake ()
        {
            if(checkpoints.Count < 3)
                Debug.LogWarning ("There are currently " + checkpoints.Count + " checkpoints set on the Track Manager.  A minimum of 3 is recommended but kart control will not be enabled with 0.");
            
            m_HistoricalBestLap = TrackRecord.Load (trackName, 1);
            m_HistoricalBestRace = TrackRecord.Load (trackName, raceLapTotal);

            popUpPreguntas.gameObject.SetActive (false);
            panelEndGame.gameObject.SetActive (false);
        }

        void OnEnable ()
        {
            for (int i = 0; i < checkpoints.Count; i++)
            {
                checkpoints[i].OnKartHitCheckpoint += CheckRacerHitCheckpoint;
            }
        }

        void OnDisable ()
        {
            for (int i = 0; i < checkpoints.Count; i++)
            {
                checkpoints[i].OnKartHitCheckpoint -= CheckRacerHitCheckpoint;
            }
        }

        void Start ()
        {
            if(checkpoints.Count == 0)
                return;
            
            Object[] allRacerArray = FindObjectsOfType<Object> ().Where (x => x is IRacer).ToArray ();

            for (int i = 0; i < allRacerArray.Length; i++)
            {
                IRacer racer = allRacerArray[i] as IRacer;
                m_RacerNextCheckpoints.Add (racer, checkpoints[0]);
                racer.DisableControl ();
            }

            m_Input = input as IInput;
            m_KartMovement = kartMovement as IKartInfo;
        }

        void Update(){
            m_StringBuilder.Clear();
            m_StringBuilder.Append("Velocidade: ");
            m_StringBuilder.Append(m_KartMovement.LocalSpeed.ToString(".##"));
            m_StringBuilder.Append(" m/s \n");
            if (tema==2){ 
                m_StringBuilder.Append("Aceleración: ");
                m_StringBuilder.Append(acelerar.ToString(".##"));
                m_StringBuilder.Append(" m/s^2 \n");
            }
            infoVelocity.text = m_StringBuilder.ToString();
            
        }

        /// <summary>
        /// Starts the timers and enables control of all racers.
        /// </summary>
        public void StartRace ()
        {
            m_IsRaceRunning = true;

            foreach (KeyValuePair<IRacer, Checkpoint> racerNextCheckpoint in m_RacerNextCheckpoints)
            {
                racerNextCheckpoint.Key.EnableControl ();
                racerNextCheckpoint.Key.UnpauseTimer ();
            }
        }

        /// <summary>
        /// Stops the timers and disables control of all racers, also saves the historical records.
        /// </summary>
        public void StopRace ()
        {
            m_IsRaceRunning = false;

            foreach (KeyValuePair<IRacer, Checkpoint> racerNextCheckpoint in m_RacerNextCheckpoints)
            {
                racerNextCheckpoint.Key.DisableControl ();
                racerNextCheckpoint.Key.PauseTimer ();
            }

            TrackRecord.Save (m_HistoricalBestLap);
            TrackRecord.Save (m_HistoricalBestRace);
        }

        void CheckRacerHitCheckpoint (IRacer racer, Checkpoint checkpoint)
        {
            if (!m_IsRaceRunning)
            {
                StartCoroutine (CallWhenRaceStarts (racer, checkpoint));
                return;
            }

            if (m_RacerNextCheckpoints.ContainsKeyValuePair (racer, checkpoint))
            {
                m_RacerNextCheckpoints[racer] = checkpoints.GetNextInCycle (checkpoint);
                RacerHitCorrectCheckpoint (racer, checkpoint);
            }
            else
            {
                RacerHitIncorrectCheckpoint (racer, checkpoint);
            }
        }

        IEnumerator CallWhenRaceStarts (IRacer racer, Checkpoint checkpoint)
        {
            while (!m_IsRaceRunning)
            {
                yield return null;
            }

            CheckRacerHitCheckpoint (racer, checkpoint);
        }

        void verifyAnswer(int[] answers){
            if (listapreguntas[answers[currentLapAnswer-1]].isOn){
                        popUpPreguntas.gameObject.SetActive (false);
                        Time.timeScale = 1f;
                        // Detener el tiempo?, una opcion
            }else{message.text = "Respuesta Incorrecta!(Estas perdiendo tiempo)";}
        }
        void loadOptionsQuestions(int racerCurrentLap, string[] questions, string[,] answersOptions){
            preguntaPopUp.text = questions[racerCurrentLap-1];
            int i = 0;
            foreach (Toggle option in listapreguntas){
                option.GetComponentInChildren<Text> ().text = answersOptions[racerCurrentLap-1,i];
                i = i + 1;
            }
        }
        public void AnswerQuestion(){
            switch (tema){
                case 1:
                    verifyAnswer(answers_mru);
                    break;
                case 2:
                    acelerar += 2f;  // No funciona bien
                    verifyAnswer(answers_mruv);
                    break;
                default:
                    break;
                
            }
        }

        void RacerHitCorrectCheckpoint (IRacer racer, Checkpoint checkpoint)
        {
            if (checkpoint.isStartFinishLine)
            {
                int racerCurrentLap = racer.GetCurrentLap ();
                currentLapAnswer = racerCurrentLap;
                if (racerCurrentLap > 0)
                {
                    float lapTime = racer.GetLapTime ();

                    if (m_SessionBestLap.time > lapTime)
                        m_SessionBestLap.SetRecord (trackName, 1, racer, lapTime);

                    if (m_HistoricalBestLap.time > lapTime)
                        m_HistoricalBestLap.SetRecord (trackName, 1, racer, lapTime);

                    if (racerCurrentLap == raceLapTotal)
                    {
                        float raceTime = racer.GetRaceTime ();

                        if (m_SessionBestRace.time > raceTime)
                            m_SessionBestRace.SetRecord (trackName, raceLapTotal, racer, raceTime);

                        if (m_HistoricalBestRace.time > raceTime)
                            m_HistoricalBestLap.SetRecord (trackName, raceLapTotal, racer, raceTime);

                        racer.DisableControl ();
                        racer.PauseTimer ();
                    }
                    if (racerCurrentLap < raceLapTotal){
                        switch (tema){
                            case 1:
                                loadOptionsQuestions(racerCurrentLap, questions_mru, answersOptions_mru);
                                break;
                            case 2:
                                loadOptionsQuestions(racerCurrentLap, questions_mruv, answersOptions_mruv);
                                break;
                            default:
                                Debug.Log("Tema incorrecto!");
                                break;
                        }
                        
                        message.text = "";
                        popUpPreguntas.gameObject.SetActive (true);
                        Time.timeScale = 0f;
                    }
                }

                if (CanEndRace ()){  
                    StopRace ();
                    panelEndGame.gameObject.SetActive (true);
                }

                racer.HitStartFinishLine ();
            }
            if (checkpoint.isFinishLine)
            {   
                StopRace ();
                panelEndGame.gameObject.SetActive (true);
                racer.HitStartFinishLine ();
            }
        }

        bool CanEndRace ()
        {
            foreach (KeyValuePair<IRacer, Checkpoint> racerNextCheckpoint in m_RacerNextCheckpoints)
            {
                if (racerNextCheckpoint.Key.GetCurrentLap () < raceLapTotal)
                    return false;
            }

            return true;
        }

        void RacerHitIncorrectCheckpoint (IRacer racer, Checkpoint checkpoint)
        {
            // No implementation required by template.
        }

        /// <summary>
        /// This function should be called when a kart gets stuck or falls off the track.
        /// It will find the last checkpoint the kart went through and reposition it there.
        /// </summary>
        /// <param name="movable">The movable representing the kart.</param>
        public void ReplaceMovable (IMovable movable)
        {
            IRacer racer = movable.GetRacer ();
            
            if(racer == null)
                return;
            
            Checkpoint nextCheckpoint = m_RacerNextCheckpoints[racer];
            int lastCheckpointIndex = (checkpoints.IndexOf (nextCheckpoint) + checkpoints.Count - 1) % checkpoints.Count;
            Checkpoint lastCheckpoint = checkpoints[lastCheckpointIndex];

            bool isControlled = movable.IsControlled ();
            movable.DisableControl ();
            kartRepositioner.OnRepositionComplete += ReenableControl;

            kartRepositioner.Reposition (lastCheckpoint, movable, isControlled);
        }

        void ReenableControl (IMovable movable, bool doEnableControl)
        {
            if(doEnableControl)
                movable.EnableControl ();
            kartRepositioner.OnRepositionComplete -= ReenableControl;
        }
    }
}