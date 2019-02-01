using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class passportControlScript : MonoBehaviour
{
    //General (Scripting)
    public KMAudio audio;
    public KMBombInfo bomb;

    //Parts (Interactables, etc..)
    public KMSelectable[] stamps;
    public GameObject[] passport;
    public GameObject ticket;

    //Variables
    public string[] forenames;
    public string[] surnames;
    public string[] ethnics;
    private string[] flights = { "Arrival", "Departure" };
    string[] sounds = { "stamp-down", "stamp-up", "printer-line" };

    /*/Restrictions/*/
    byte flightRestr = 0;
    bool ethnicRestr = false;
    bool ageRestr = false;
    bool allPass = false;
    bool allDeny = false;
    int[] date = { 0, 0, 2000 };
    List<string> activeRestrs = new List<string>();
    bool rules = false;

    /*/Gameplay/*/
    bool passAns = true;
    bool passUser = false;
    int passages = 0;
    string name;
    string ethnicity;
    int[] birthday = { 0, 0, 1900 };
    int[] expiration = { 0, 0, 2000 };
    string flightType;

    //Logging
    static int moduleCounter = 1;
    int moduleId;
    private bool moduleSolved;

    //Initializing Module
    void Awake()
    {
        moduleId = moduleCounter++;

        foreach(KMSelectable stamp in stamps)
        {
            stamp.OnInteract += delegate () { Permit(stamp); return false; };
        }
    }

    //Module Code
	void Start()
    {
        RuleSet();
        CreatePassenger();
        CheckPassenger();
    }


    //Methods
    void Permit(KMSelectable stamp)
    {
        if(moduleSolved)
        {
            return;
        }
        stamp.AddInteractionPunch();
        if (stamp == stamps[0])
        {
            passUser = true;
        }
        else
        {
            passUser = false;
        }
        Debug.LogFormat("[Passport Control #{0}] You pressed {1}, expected {2}! {3}", moduleId, passUser?"approve":"deny", passAns?"approve":"deny", passAns==passUser?"Correct!":"Wrong");

        StartCoroutine(ButtonPress(passUser == passAns));
        //checking input
        if (passUser == passAns)
        {
            //correct
            passages++;
            if(passages == 3)
            {
                //solve
                moduleSolved = true;
                GetComponent<KMBombModule>().HandlePass();
                Debug.LogFormat("[Passport Control #{0}] Module Solved.", moduleId);
            }
            else
            {
                Start();
            }
        }
        else
        {
            //strike & reset
            GetComponent<KMBombModule>().HandleStrike();
            Start();
        }


    }

    void CreatePassenger()
    {
        name = forenames[UnityEngine.Random.Range(0, forenames.Length)] + " " + surnames[UnityEngine.Random.Range(0, surnames.Length)];
        ethnicity = ethnics[UnityEngine.Random.Range(0, ethnics.Length)];
        flightType = flights[UnityEngine.Random.Range(0, flights.Length)];

        //birthday
        birthday[0] = UnityEngine.Random.Range(1, 30);
        birthday[1] = UnityEngine.Random.Range(1, 13);
        birthday[2] = 1900 + UnityEngine.Random.Range(50, 101);

        //expiration
        expiration[0] = UnityEngine.Random.Range(1, 30);
        expiration[1] = UnityEngine.Random.Range(1, 13);
        expiration[2] = 2000 + UnityEngine.Random.Range(-10, 20);

        passport[0].GetComponent<TextMesh>().text = name;
        passport[1].GetComponent<TextMesh>().text = ethnicity;
        passport[2].GetComponent<TextMesh>().text = birthday[0].ToString() + "/" + birthday[1].ToString() + "/" + birthday[2].ToString();
        passport[3].GetComponent<TextMesh>().text = expiration[0].ToString() + "/" + expiration[1].ToString() + "/" + expiration[2].ToString();
        ticket.GetComponent<TextMesh>().text = flightType;
        Debug.LogFormat("[Passport Control #{0}] Curret citizen: {1} | Born on: {2}/{3}/{4} | Ethnicity: {5} | Flight status: {6}", moduleId, name, birthday[0], birthday[1], birthday[2], ethnicity, flightType);
    }

    void RuleSet()
    {
        if (rules)
        {
            return;
        }

        int i = 0;
        char currCH = ' ';
        //unicorn rule 1
        if(bomb.IsIndicatorOn("BOB") && bomb.IsIndicatorOn("CAR") && bomb.IsIndicatorOn("NSA"))
        {
            allPass = true;
            Debug.LogFormat("[Passport Control #{0}] All-pass rule active, expecting all passengers through", moduleId);
        }
        //unicorn rule 2
        else if(bomb.IsIndicatorOff("BOB") && bomb.IsIndicatorOn("CAR") && bomb.GetPortCount(Port.PS2) > 0)
        {
            allDeny = true;
            Debug.LogFormat("[Passport Control #{0}] All-deny rule active, expecting no passengers through", moduleId);
        }
        //normal rules
        else
        {
            //flight restriction (arrivals/departures/none)
            if (bomb.GetBatteryCount() >= 3 && bomb.GetPortPlates().Any(x => x.Contains("Parallel") && x.Contains("Serial")))
            {
                //arrival only
                flightRestr = 1;
                activeRestrs.Add("Arrivals only");
            }
            else if (bomb.GetBatteryCount() <= 2 && bomb.IsIndicatorOn("SND"))
            {
                //departure only
                flightRestr = 2;
                activeRestrs.Add("Departures only");
            }
            else
            {
                Debug.Log("No flight restrictions");
            }

            //ethnicity restriction
            foreach(char letter in bomb.GetSerialNumberLetters())
            {
                if("ARSTOZKA".Contains(letter))
                {
                    i++;
                }
            }
            if(i >= 3)
            {
                ethnicRestr = true;
                activeRestrs.Add("Arstotzkans only");
            }

            //age restriction
            if(bomb.GetSerialNumberNumbers().Sum() >= 18)
            {
                ageRestr = true;
                activeRestrs.Add("18+ only");
            }
            i = 0;

            //Date creation
            /*/Day/*/
            currCH = bomb.GetSerialNumber()[0];
            if(currCH >= '0' && currCH <= '9')
            {
                date[0] = ((currCH - '0') % 3) * 10;
            }
            else
            {
                date[0] = ((currCH - 'A' + 1) % 3) * 10;
            }
            currCH = bomb.GetSerialNumber()[1];
            if(currCH >= '0' && currCH <= '9')
            {
                date[0] += (currCH - '0') % 10;
            }
            else
            {
                date[0] += (currCH - 'A' + 1) % 10;
            }
            if(date[0] == 0)
            {
                date[0]++;
            }

            /*/Month/*/
            currCH = bomb.GetSerialNumber()[2];
            if(currCH >= '0' && currCH<= '9')
            {
                i += currCH - '0';
            }
            else
            {
                i += currCH - 'A' + 1;
            }
            currCH = bomb.GetSerialNumber()[3];
            if (currCH >= '0' && currCH <= '9')
            {
                i += currCH - '0';
            }
            else
            {
                i += currCH - 'A' + 1;
            }
            date[1] = i % 12 + 1;

            i = 0;
            /*/Year/*/
            currCH = bomb.GetSerialNumber()[4];
            if (currCH >= '0' && currCH <= '9')
            {
                i += (currCH - '0') % 3 * 10;
            }
            else
            {
                i += (currCH - 'A' + 1) % 10 % 3 * 10;
            }
            currCH = bomb.GetSerialNumber()[5];
            if (currCH >= '0' && currCH <= '9')
            {
                i += currCH - '0';
            }
            else
            {
                i += (currCH - 'A' + 1) % 10;
            }
            date[2] = 2000 + (i % 30);
            Debug.LogFormat("[Passport Control #{0}] Active restrictions are: {1}", moduleId, activeRestrs.ToArray().Length > 0?string.Join(", ", activeRestrs.ToArray()):"None");
            Debug.LogFormat("[Passport Control #{0}] Date: {1}/{2}/{3}", moduleId, date[0], date[1], date[2]);
        }
        rules = true;
    }

    void CheckPassenger()
    {
        passAns = true;
        if(allPass)
        {
            return;
        }
        else if(allDeny)
        {
            passAns = false;
        }
        else
        {
            if(flightRestr > 0)
            {
                if(flightType != flights[flightRestr-1])
                {
                    passAns = false;
                }
            }
            if(ethnicRestr)
            {
                if(ethnicity != ethnics[0])
                {
                    passAns = false;
                }
            }
            if(ageRestr)
            {
                //year margin of exactly 18
                if(date[2]-birthday[2] == 18)
                {
                    //same month
                    if(date[1] == birthday[1])
                    {
                        //today is an earlier day
                        if(date[0] < birthday[0])
                        {
                            passAns = false;
                        }
                    }
                    //earlier month
                    else if (date[1] < birthday[1])
                    {
                        passAns = false;
                    }
                }
                //less than 18 year margin
                else if (date[2] - birthday[2] < 18)
                {
                    passAns = false;
                }
            }
            //expired, by year
            if(expiration[2] < date[2])
            {
                passAns = false;
                Debug.LogFormat("[Passport Control #{0}] Expired passport!", moduleId);
            }                   
            //expired, by month 
            else if(expiration[2] == date[2] && expiration[1] < date[1])
            {                   
                passAns = false;
                Debug.LogFormat("[Passport Control #{0}] Expired passport!", moduleId);
            }                   
            //expired, by day   
            else if(expiration[2] == date[2] && expiration[1] == date[1] && expiration[0] < date[0])
            {                   
                passAns = false;
                Debug.LogFormat("[Passport Control #{0}] Expired passport!", moduleId);
            }
        }
        Debug.LogFormat("[Passport Control #{0}] Citizen should be {1}", moduleId, passAns ? "Approved" : "Denied");
    }

    IEnumerator ButtonPress(bool correct)
    {
        audio.PlaySoundAtTransform("stamp-down", transform);
        yield return new WaitForSeconds(0.3f);
        audio.PlaySoundAtTransform("stamp-up", transform);
        yield return new WaitForSeconds(0.2f);
        if(!correct)
        {
            audio.PlaySoundAtTransform("printer-line", transform);
        }
    }
}
