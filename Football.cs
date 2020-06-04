using HQMEditorDedicated;
using System;
using System.Diagnostics;
using System.Timers;
using System.Linq;
using System.Collections.Generic;

namespace Football
{
    public class Football
    {
        public static bool FaceOffState2
        {
            get { return MemoryEditor.ReadInt(0x7D348A0) == 1; }
            set { MemoryEditor.WriteInt(value ? 1 : 0, 0x7D348A0); }
        }
        EOffsideState m_OffsideState = EOffsideState.None;
        bool m_OffsideCalled = false;
        enum EOffsideState
        {
            None,
            Red,
            Blue
        }
        static bool puck_enabled = true;
        float m_LastPuckZ = Puck.Position.Z;
        int LastTeamToTouch = 0;
        int lastPlayerToTouch = 0;
        bool faceoffstate = false;
        bool faceoff = false;
        bool p_Colided = false;
        public static void PuckCollision(bool value)
        {

            byte[] disable1_false = new byte[5] { 0xE8, 0x90, 0x52, 0x00, 0x00 };
            byte[] disable1_true = new byte[5] { 0x90, 0x90, 0x90, 0x90, 0x90 };

            byte[] disable2_false = new byte[5] { 0xE8, 0xFB, 0x52, 0x00, 0x00 };
            byte[] disable2_true = new byte[5] { 0x90, 0x90, 0x90, 0x90, 0x90 };

            byte[] disableph_false = new byte[5] { 0xE8, 0xA5, 0x54, 0x00, 0x00 };
            byte[] disableph_true = new byte[5] { 0x90, 0x90, 0x90, 0x90, 0x90 };

            int disable1 = 0x0040C5AB;
            int disable2 = 0x0040C540;
            int disableph = 0x0040C396;

            if (value && puck_enabled)
            {
                Debug.Print("collision disabled");
                MemoryEditor.WriteBytes(disable1_true, disable1);
                MemoryEditor.WriteBytes(disable2_true, disable2);
                MemoryEditor.WriteBytes(disableph_true, disableph);
                Puck.Position = new HQMVector(Puck.Position.X, 0.2f, Puck.Position.Z);
                Puck.Velocity = HQMVector.Zero;
                Puck.RotationalVelocity = HQMVector.Zero;
                puck_enabled = false;
            }
            if (!value && !puck_enabled)
            {
                Debug.Print("collision enabled");
                MemoryEditor.WriteBytes(disable1_false, disable1);
                MemoryEditor.WriteBytes(disable2_false, disable2);
                MemoryEditor.WriteBytes(disableph_false, disableph);
                puck_enabled = true;
            }
        }

        public static float Distance2D(HQMVector v1, HQMVector v2)
        {
            return ((v1.X - v2.X) * (v1.X - v2.X) + (v1.Z - v2.Z) * (v1.Z - v2.Z));
        }
        float last_red_position = 0f;
        float last_blue_position = 0f;
        int touching = 0;
        string lastplaye = "";

        public List<int> FindRedsOffside(float value)
        {
            Player[] players = PlayerManager.PlayersInServer;
            var reds_offside = players.Where(x => x.Team == HQMTeam.Red).Where(x => x.Role != HQMRole.G).Where(v => v.Position.Z < value).Select(x => x.OnIceID).DefaultIfEmpty().ToList();
            Debug.Print(reds_offside[0].ToString());
            return reds_offside;

        }
        Player FindLastBluePlayer()
        {
            Player[] players = PlayerManager.PlayersInServer;
            var last_blue_position = players.Where(x => x.Team == HQMTeam.Blue).Where(x => x.Role != HQMRole.G).OrderByDescending(v => v.Position.Z).First();
            return last_blue_position;
        }

        List<int> offsideplayers = new List<int>();
        public void CheckForOffside()
        {
            float curPuckZ = Puck.Position.Z;
            Player[] players = PlayerManager.Players;
            float pos = FindLastBluePlayer().Position.Z;
            //0x7D3495C - id of last player to touch the puck
            if (FindRedsOffside(pos).Contains(MemoryEditor.ReadInt(0x7D3495C)) && !m_OffsideCalled)
            {
                Chat.SendMessage("Offside!");
                m_OffsideCalled = true;
                PuckCollision(true);
                //m_OffsideCalled = false;
            }
            //touching = MemoryEditor.ReadInt(0x1893E1C);
            /*
            if (lastPlayerToTouch != MemoryEditor.ReadInt(0x7D3495C) && !m_OffsideCalled)
            {
                lastPlayerToTouch = MemoryEditor.ReadInt(0x7D3495C);
                if (LastTeamToTouch == 0)
                {
                    float pos = FindLastBluePlayer().Position.Z;
                    if (FindRedsOffside(pos).Contains(MemoryEditor.ReadInt(0x7D3495C)))
                    {
                        Chat.SendMessage("Offside!");
                        m_OffsideCalled = true;
                        PuckTempDisable();
                    }
                }
            }
            */
            System.Threading.Thread.Sleep(500);
        }
        public void Detection()
        {
            LastTeamToTouch = MemoryEditor.ReadInt(0x7D34958);
            faceoffstate = Convert.ToBoolean(MemoryEditor.ReadInt(0x7D33C9C));
            if (Puck.Position.X >= 74.7f)
            {
                Puck.Position = new HQMVector(74.6f, Puck.Position.Y, Puck.Position.Z);
                ThrowIn();
                ResetPuck();
            }
            if (Puck.Position.X <= 4.4f)
            {
                Puck.Position = new HQMVector(4.5f, Puck.Position.Y, Puck.Position.Z);
                ThrowIn();
                ResetPuck();
            }
            //blue goalkick
            if (Puck.Position.X >= 43.38f | Puck.Position.X <= 35.6f && Puck.Position.Z <= 3.8f && LastTeamToTouch == 0)
            {
                Chat.SendMessage("Blue Goal Kick");
                Puck.Position = new HQMVector(39.5f, Puck.Position.Y, 10f);
                BlueGoalKick();
                ResetPuck();
            }

            //behind blue goal goalkick
            if (Puck.Position.X >= 35.6f && Puck.Position.X <= 43.38f && Puck.Position.Z <= 1.2f && LastTeamToTouch == 0)
            {
                Chat.SendMessage("Blue Goal Kick");
                Puck.Position = new HQMVector(39.5f, Puck.Position.Y, 10f);
                BlueGoalKick();
                ResetPuck();
            }
            //behind blue goal red cornerkick
            if (Puck.Position.X >= 35.6f && Puck.Position.X <= 43.38f && Puck.Position.Z <= 1.2f && LastTeamToTouch == 1)
            {
                Chat.SendMessage("Red Corner");
                Puck.Position = new HQMVector(74.1f, Puck.Position.Y, 4.6f);
                RedCorner();
                ResetPuck();
            }
            //red cornerkick 1
            if (Puck.Position.X >= 43.38f && Puck.Position.Z <= 3.8f && LastTeamToTouch == 1)
            {
                Chat.SendMessage("Red Corner");
                Puck.Position = new HQMVector(74.1f, Puck.Position.Y, 4.6f);
                RedCorner();
                ResetPuck();
            }
            //red cornerkick 2
            if (Puck.Position.X <= 35.6f && Puck.Position.Z <= 3.8f && LastTeamToTouch == 1)
            {
                Chat.SendMessage("Red Corner");
                Puck.Position = new HQMVector(5.2f, Puck.Position.Y, 4.6f);
                RedCorner();
                ResetPuck();
            }
            //behind red goal red goalkick
            if (Puck.Position.X >= 35.6f && Puck.Position.X <= 43.38f && Puck.Position.Z >= 112f && LastTeamToTouch == 1)
            {
                Chat.SendMessage("Read Goal Kick");
                Puck.Position = new HQMVector(39.5f, Puck.Position.Y, 100f);
                RedGoalKick();
                ResetPuck();
            }
            //behind red goal blue cornerkick
            if (Puck.Position.X >= 35.6f && Puck.Position.X <= 43.38f && Puck.Position.Z >= 112f && LastTeamToTouch == 0)
            {
                Chat.SendMessage("Blue Corner");
                Puck.Position = new HQMVector(73.8f, Puck.Position.Y, 108.5f);
                BlueCorner();
                ResetPuck();
            }
            //red goal kick
            if (Puck.Position.X >= 43.38f | Puck.Position.X <= 35.6f && Puck.Position.Z >= 109.2f && LastTeamToTouch == 1)
            {
                Chat.SendMessage("Red Goal Kick");
                Puck.Position = new HQMVector(39.5f, Puck.Position.Y, 100f);
                RedGoalKick();
                ResetPuck();
            }
            //blue cornerkick
            if (Puck.Position.X >= 43.38f && Puck.Position.Z >= 109.2f && LastTeamToTouch == 0)
            {
                Chat.SendMessage("Blue Corner");
                Puck.Position = new HQMVector(73.8f, Puck.Position.Y, 108.5f);
                BlueCorner();
                ResetPuck();
            }
            //blue cornerkick 2
            if (Puck.Position.X <= 35.6f && Puck.Position.Z >= 109.2f && LastTeamToTouch == 0)
            {
                Chat.SendMessage("Blue Corner");
                Puck.Position = new HQMVector(5.2f, Puck.Position.Y, 108.5f);
                BlueCorner();
                ResetPuck();
            }
            if (!faceoffstate && FaceOffState2 && faceoff)
            {
                Debug.Print("try to enable puck");
                PuckCollision(false);
                FaceOffState2 = false;
                faceoff = false;
            }
            if (MemoryEditor.ReadInt(0x7D3489C) == 1 && faceoff)
            {
                Debug.Print("try to enable puck");
                PuckCollision(false);
                MemoryEditor.WriteInt(0, 0x7D3489C);
                faceoff = false;
            }
            //warmuptime gamestate
            if (GameInfo.IntermissionTime > 100 && !GameInfo.GameState && !faceoff)
            {
                Debug.Print("new half kickoff");
                Chat.SendMessage("Kick Off in 20s");
                ResetPuck();
                Puck.Position = new HQMVector(39.5f, 0.1f, 56.5f); //center
                PuckCollision(true);
                faceoff = true;
            }    
            if (faceoffstate && GameInfo.IntermissionTime <= 1800 && !faceoff)
            {
                Debug.Print("after goal kickoff");
                Chat.SendMessage("Kick Off in 20s");
                FaceOffState2 = true;
                ResetPuck();
                Puck.Position = new HQMVector(39.5f, 0.1f, 56.5f); //center
                PuckCollision(true);
                faceoff = true;
            }
            System.Threading.Thread.Sleep(500);
        }

        public void BlueGoalKick()
        {
            GameInfo.AfterGoalFaceoffTime = 3500;
            PuckTempDisable();
        }
        public void RedGoalKick()
        {
            GameInfo.AfterGoalFaceoffTime = 5500;
            PuckTempDisable();
        }
        public void RedCorner()
        {
            GameInfo.AfterGoalFaceoffTime = 4500;
            PuckTempDisable();
        }
        public void BlueCorner()
        {
            GameInfo.AfterGoalFaceoffTime = 6500;
            PuckTempDisable();
        }
        public void ThrowIn()
        {
            if (LastTeamToTouch == 0)
            {
                Chat.SendMessage("Blue Throw-in");
                GameInfo.AfterGoalFaceoffTime = 7500;
                PuckTempDisable();
            }
            else
            {
                Chat.SendMessage("Red Throw-in");
                GameInfo.AfterGoalFaceoffTime = 8500;
                PuckTempDisable();
            }
        }

        public void PuckTempDisable()
        {
            PuckCollision(true);
            Debug.Print("puck disabled");
            System.Threading.Thread.Sleep(8000);
            Debug.Print("puck enabled");
            PuckCollision(false); 
            GameInfo.AfterGoalFaceoffTime = 0;
        }
        public void ResetPuck()
        {
            MemoryEditor.WriteFloat(1f, 0x187B708);
            MemoryEditor.WriteFloat(0f, 0x187B70C);
            MemoryEditor.WriteFloat(0f, 0x187B710);
            MemoryEditor.WriteFloat(0f, 0x187B714);
            MemoryEditor.WriteFloat(1f, 0x187B718);
            MemoryEditor.WriteFloat(0f, 0x187B71C);
            MemoryEditor.WriteFloat(0f, 0x187B720);
            MemoryEditor.WriteFloat(0f, 0x187B72C);
            MemoryEditor.WriteFloat(0f, 0x187B730);
            MemoryEditor.WriteFloat(0f, 0x187B734);
            MemoryEditor.WriteFloat(0f, 0x187B724);
            MemoryEditor.WriteFloat(1f, 0x187B728);
            MemoryEditor.WriteFloat(0f, 0x187B744);
            MemoryEditor.WriteFloat(0f, 0x187B748);
            MemoryEditor.WriteFloat(0f, 0x187B74C);
        }
        public void CheckForBodyPuckC()
        {
            foreach (Player p in PlayerManager.Players)
            {
                if (Football.Distance2D(p.Position, Puck.Position) < 0.3f && Puck.Position.Y > p.Position.Y && Puck.Position.Y < p.Position.Y + 0.5)
                {
                    if (!p_Colided)
                    {
                        Puck.Velocity = HQMVector.Zero;
                        //TODO: play with vectors
                        //try to to rebound the puck
                        //Puck.Velocity = (p.Position - Puck.Position).Normalized * 0.1f;
                        p_Colided = true;
                        System.Threading.Thread.Sleep(500);
                    }
                    
                }
                else
                {
                    p_Colided = false;
                }
            }
        }
    }
}
