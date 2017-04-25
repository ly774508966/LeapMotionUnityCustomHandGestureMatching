using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Leap;
using System.IO;

public class HandMatchingScript : MonoBehaviour {

    Controller controller;

    public AudioClip buzzSound;
    public AudioClip matchSound;
    private AudioSource source;
    private float volLowRange = .5f;
    private float volHighRange = 1.0f;

    Leap.Vector fprevDirection = Leap.Vector.Zero;
    Leap.Vector fprevPalmPosition = Leap.Vector.Zero;
    Leap.Vector fprevPalmNormal = Leap.Vector.Zero;
    Leap.Vector fprevHandVelocity = Leap.Vector.Zero;

    Leap.Vector sprevDirection = Leap.Vector.Zero;
    Leap.Vector sprevPalmPosition = Leap.Vector.Zero;
    Leap.Vector sprevPalmNormal = Leap.Vector.Zero;
    Leap.Vector sprevHandVelocity = Leap.Vector.Zero;


    float totalDistance = 0.0f;
    float totalPalmNormal = 0.0f;
    float totalPalmPosition = 0.0f;
    float totalVelocity = 0.0f;

    long frameCount = 0;

    float fdistanceDirection;
    float fdistancePalmPosition;
    float fdistancePalmNormal;
    float fdistanceHandVelocity;

    float sdistanceDirection;
    float sdistancePalmPosition;
    float sdistancePalmNormal;
    float sdistanceHandVelocity;
    StreamWriter directionFile;


    public Text status_text;
    private string test_status = "Wow starting ...";


    // Use this for initialization
    void Start()
    {
        controller = new Controller();

        controller.EnableGesture(Gesture.GestureType.TYPESWIPE);

        source = GetComponent<AudioSource>();

        directionFile = new StreamWriter(@"report.txt", true);

        frameCount = 0;
        totalDistance = 0.0f;
        totalPalmNormal = 0.0f;
        totalPalmPosition = 0.0f;
        totalVelocity = 0.0f;

        status_text.text = "yeah " + test_status;

    }

    // Update is called once per frame
    void Update()
    {
        Frame frame = controller.Frame();
        Debug.Log("Frame Id: " + frame.Id);
        frameCount++;

        HandList hands = frame.Hands;
        for (int i = 0; i < hands.Count; i++)
        {
            Hand hand = hands[i];
            Leap.Vector direction = hand.Direction;
            Leap.Vector palmNormal = hand.PalmNormal;
            Leap.Vector palmPosition = hand.PalmPosition;
            Leap.Vector handSpeed = hand.PalmVelocity;


            if (i == 0)
            {
                fdistanceDirection = direction.DistanceTo(fprevDirection);
                fdistancePalmPosition = palmPosition.DistanceTo(fprevPalmPosition);
                fdistancePalmNormal = palmNormal.DistanceTo(fprevPalmNormal);
                fdistanceHandVelocity = handSpeed.DistanceTo(fprevHandVelocity);

                fprevDirection = direction;
                fprevPalmNormal = palmNormal;
                fprevPalmPosition = palmPosition;
                fprevHandVelocity = handSpeed;

            }
            else if (i == 1)
            {
                sdistanceDirection = direction.DistanceTo(sprevDirection);
                sdistancePalmPosition = palmPosition.DistanceTo(sprevPalmPosition);
                sdistancePalmNormal = palmNormal.DistanceTo(sprevPalmNormal);
                sdistanceHandVelocity = handSpeed.DistanceTo(sprevHandVelocity);

                sprevDirection = direction;
                sprevPalmNormal = palmNormal;
                sprevPalmPosition = palmPosition;
                sprevHandVelocity = handSpeed;

                Debug.Log("first direction " + fdistanceDirection);
                Debug.Log("second direction " + sdistanceDirection);

                Debug.Log("Direction change = " + (fdistanceDirection - sdistanceDirection));
                directionFile.WriteLine("");
                directionFile.WriteLine("frame id: " + frame.Id);
                directionFile.WriteLine("Direction first: " + fdistanceDirection + " second: " + sdistanceDirection + " diff: " + (fdistanceDirection - sdistanceDirection));
                directionFile.WriteLine("PalmPosition first: " + fdistancePalmPosition + " second: " + sdistancePalmPosition + " diff: " + (fdistancePalmPosition - sdistancePalmPosition));
                directionFile.WriteLine("PalmNormal first: " + fdistancePalmNormal + " second: " + sdistancePalmNormal + " diff: " + (fdistancePalmNormal - sdistancePalmNormal));
                directionFile.WriteLine("Velocity first: " + fdistanceHandVelocity + " second: " + sdistanceHandVelocity + " diff: " + (fdistanceHandVelocity - sdistanceHandVelocity));

                directionFile.WriteLine("");
                float directionChange = (fdistanceDirection - sdistanceDirection);
                float palmPositionChange = fdistancePalmPosition - sdistancePalmPosition;
                float palmNormalChange = fdistancePalmNormal - sdistancePalmNormal;
                float velocityChange = fdistanceHandVelocity - sdistanceHandVelocity;

                if (directionChange < 0.0f) directionChange *= -1.0f;
                if (palmPositionChange < 0.0f) palmPositionChange *= -1.0f;
                if (palmNormalChange < 0.0f) palmNormalChange *= -1.0f;
                if (velocityChange < 0.0f) velocityChange *= -1.0f;

                totalDistance += directionChange;
                totalPalmPosition += palmPositionChange;
                totalPalmNormal += palmNormalChange;
                totalVelocity += velocityChange;
                test_status = "Status:\n";
                if (directionChange < 0.001)
                {
                    test_status += "Direction OK\n";
                }
                else
                {
                    test_status += "Fix Direction\n";
                }

                if (palmPositionChange < 1.0)
                {
                    test_status += "PalmPosition OK\n";
                }
                else
                {
                    test_status += "Fix PalmPosition\n";
                }

                if (palmNormalChange < 1.0)
                {
                    test_status += "PalmNormal OK\n";
                }
                else
                {
                    test_status += "Fix PalmNormal\n";
                }

                if (velocityChange < 1.0)
                {
                    test_status += "Velocity OK\n";
                }
                else
                {
                    test_status += "Fix Velocity\n";
                }

                float error = totalDistance * 2.0f + totalPalmNormal * 2.0f + totalVelocity + totalPalmPosition * 3.0f;
                error /= (4 * frameCount);

                var message = new SharpOSC.OscMessage("/hand/1", error);
                var sender = new SharpOSC.UDPSender("127.0.0.1", 55555);
                sender.Send(message);

                if (error > 5.0)
                {
                    status_text.color = Color.red;
                    test_status += "Not matching";
                    //source.PlayOneShot(buzzSound);
                }
                else
                {
                    status_text.color = Color.green;
                    test_status += "Matching";
                    //source.PlayOneShot(matchSound);
                }
                    

                status_text.text = test_status;
            }

        }
    }

    void OnApplicationQuit()
    {
        float error = totalDistance * 2.0f + totalPalmNormal * 2.0f + totalVelocity + totalPalmPosition * 3.0f;
        error /= (4 * frameCount);
        string resultText = "MATCH";

        if (error > 5.0)
            resultText = "MISMATCH";

        directionFile.WriteLine("-----------------------");
        directionFile.WriteLine("ERROR : " + error);
        directionFile.WriteLine("Qualitative result : " + resultText);
        directionFile.WriteLine("------------------------");

        Debug.Log("Application ending after " + Time.time + " seconds");
        directionFile.Close();
    }
}
