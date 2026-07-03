using UnityEngine;
using System.Collections.Generic;

public class TrainGenerator : MonoBehaviour
{
    public float speed = 12.0f;
    public float intervalMin = 25.0f;
    public float intervalMax = 40.0f;
    public float spawnX = -60.0f;
    public float destroyX = 60.0f;
    public GameObject trainPrefab;

    public AudioClip warningSignal;
    public AudioSource audioSource;
    private bool warningPlayed = false;

    public RailFlasher railFlasher;

    private float nextTrainTime;
    private float timer = 0f;

    private List<GameObject> trains;

    public void Start()
    {
        if (trains == null)
            trains = new List<GameObject>(4);
        else
            trains.Clear();

        ScheduleNextTrain();
        UpdateAudioVolumeFromPrefs();
    }

    public void Update()
    {
        timer += Time.deltaTime;

        UpdateAudioVolumeFromPrefs();

        if (!warningPlayed && timer >= nextTrainTime - 5f)
        {
            PlayWarningSignal();
        }

        if (timer >= nextTrainTime)
        {
            timer = 0f;
            warningPlayed = false;
            ScheduleNextTrain();
            SpawnTrain();
        }

        for (int i = trains.Count - 1; i >= 0; i--)
        {
            GameObject train = trains[i];

            if (train == null)
            {
                trains.RemoveAt(i);
                continue;
            }

            if (train.transform.position.x > destroyX + 10f)
            {
                Destroy(train);
                trains.RemoveAt(i);
            }
        }
    }

    private void ScheduleNextTrain()
    {
        nextTrainTime = Random.Range(intervalMin, intervalMax);
    }

    private void SpawnTrain()
    {
        Vector3 spawnPosition = new Vector3(spawnX, 0.6f, transform.position.z + 1.8f);
        GameObject train = Instantiate(trainPrefab, spawnPosition, Quaternion.Euler(0, 90, 0));

        TrainMover mover = train.GetComponent<TrainMover>();
        if (mover == null)
            mover = train.AddComponent<TrainMover>();

        mover.Initialize(speed);
        trains.Add(train);

        if (railFlasher != null)
            railFlasher.StopFlashing();
    }

    private void PlayWarningSignal()
    {
        if (warningSignal != null && audioSource != null)
            audioSource.Play();

        if (railFlasher != null)
            railFlasher.StartFlashing();

        warningPlayed = true;
    }

    private void UpdateAudioVolumeFromPrefs()
    {
        if (audioSource == null) return;

        int soundLevel = PlayerPrefs.GetInt("soundLevel", 3);
        float volume = 0f;

        switch (soundLevel)
        {
            case 0: volume = 0f; break;
            case 1: volume = 0.25f; break;
            case 2: volume = 0.5f; break;
            case 3: volume = 1f; break;
        }

        audioSource.volume = volume;
    }

    public void OnDestroy()
    {
        if (trains == null)
            return;

        for (int i = trains.Count - 1; i >= 0; i--)
        {
            if (trains[i] != null)
                Destroy(trains[i]);
        }
    }
}
