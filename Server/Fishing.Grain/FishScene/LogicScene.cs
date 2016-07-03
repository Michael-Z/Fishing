using System;
using System.Collections;
using System.Collections.Generic;
using GF.Unity.Common;

namespace Fishing
{
    public interface ILogicListener
    {
        //---------------------------------------------------------------------
        void onLogicScene2RenderAll(List<string> vec_param);

        //---------------------------------------------------------------------
        void onLogicScene2Render(uint et_player_rpcid, List<string> vec_param);

        //---------------------------------------------------------------------
        void onLogicSceneSetPlayerGold(uint et_player_rpcid, int new_gold, int fish_vibid, string reason, int turret_rate);

        //---------------------------------------------------------------------
        int onLogicSceneGetPlayerGold(uint et_player_rpcid);

        //---------------------------------------------------------------------
        void onLogicSceneFishDie(uint et_player_rpcid, int fish_vibid, int total_score);

        //---------------------------------------------------------------------
        void onLogicSceneFishHit(uint et_player_rpcid, int fish_vibid, int turret_rate);
    }

    public class CLogicScene : IDisposable
    {
        //---------------------------------------------------------------------
        CEffectMgr mEffectMgr = null;
        CSceneBox mSceneBox = null;// ������
        CLogicProtocol mProtocol = null;
        CLogicLevel mLevel = null;// �ؿ�
        Dictionary<uint, CLogicTurret> mMapPlayerTurret = new Dictionary<uint, CLogicTurret>();// key=et_player_rpcid
        ILogicListener mListener = null;
        List<int> mListTurretRate = new List<int>();
        bool mbSingle = false;// �Ƿ��ǵ���ģʽ
        bool mbFishMustDie = false;
        float mfPumpingRate = 1.0f;
        TagColliderMgr mColliderMgr = null;

        //---------------------------------------------------------------------
        public CLogicScene()
        {
        }

        //---------------------------------------------------------------------
        ~CLogicScene()
        {
            this.Dispose(false);
        }

        //---------------------------------------------------------------------
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        //---------------------------------------------------------------------
        protected virtual void Dispose(bool disposing)
        {
            destroy();
        }

        //---------------------------------------------------------------------
        public void create(int default_level_vibid, float level_rate, bool single, bool fish_mustdie,
            ILogicListener listener, float pumping_rate, List<int> list_turret_rate,
            List<JsonPacket> json_packet_list, List<RouteJsonPacket> route_json_packet_list)
        {
            mProtocol = new CLogicProtocol(this);
            mSceneBox = new CSceneBox();
            mListener = listener;
            mbSingle = single;
            mfPumpingRate = pumping_rate;
            mbFishMustDie = fish_mustdie;
            mListTurretRate = list_turret_rate;

            // ��ʼ���ؿ�
            if (default_level_vibid != -1)
            {
                mLevel = new CLogicLevel(this);
                mLevel.create(default_level_vibid, level_rate, json_packet_list, route_json_packet_list);
            }

            // ��ʼ����Ч������
            mEffectMgr = new CEffectMgr();
            mEffectMgr.regEffectFactory(new EffectSpreadFishFactory());
            mEffectMgr.regEffectFactory(new EffectTimeStopFactory());
            mEffectMgr.regEffectFactory(new LogicEffectLightingChainFactory());
            mEffectMgr.regEffectFactory(new LogicEffectFullScreenBombFactory());
            mEffectMgr.regEffectFactory(new LogicEffectAOEFactory());

            mColliderMgr = new TagColliderMgr();
        }

        //---------------------------------------------------------------------
        public void destroy()
        {
            if (mLevel != null)
            {
                mLevel.destroy();
                mLevel = null;
            }

            foreach (var i in mMapPlayerTurret)
            {
                i.Value.Dispose();
            }
            mMapPlayerTurret.Clear();

            if (mSceneBox != null)
            {
                mSceneBox = null;
            }

            if (mProtocol != null)
            {
                mProtocol.Dispose();
                mProtocol = null;
            }

            if (mColliderMgr != null)
            {
                mColliderMgr.destroy();
                mColliderMgr = null;
            }
        }

        //---------------------------------------------------------------------
        public void update(float elapsed_tm)
        {
            // ������ײ��
            mColliderMgr.update();

            // ����������̨
            foreach (var i in mMapPlayerTurret)
            {
                i.Value.update(elapsed_tm);
            }

            // ���¹ؿ�
            if (mLevel != null)
            {
                mLevel.update(elapsed_tm);
            }

            // ������Ч������
            mEffectMgr.update(elapsed_tm);

            // ��������Э�������
            mProtocol.update(elapsed_tm);
        }

        //---------------------------------------------------------------------
        public void sceneOnRecvFromRender(List<string> vec_param)
        {
            mProtocol.onRecv(vec_param);
        }

        //---------------------------------------------------------------------
        // �Ƿ��ǵ���ģʽ
        public bool isSingleMode()
        {
            return mbSingle;
        }

        //---------------------------------------------------------------------
        public CLogicProtocol getProtocol()
        {
            return mProtocol;
        }

        //---------------------------------------------------------------------
        public TagColliderMgr getColliderMgr()
        {
            return mColliderMgr;
        }

        //---------------------------------------------------------------------
        public CLogicLevel getLevel()
        {
            return mLevel;
        }

        //---------------------------------------------------------------------
        public CSceneBox getSceneBox()
        {
            return mSceneBox;
        }

        //---------------------------------------------------------------------
        public float getSceneLength()
        {
            return CCoordinate.LogicSceneLength;
        }

        //---------------------------------------------------------------------
        public float getSceneWidth()
        {
            return CCoordinate.LogicSceneWidth;
        }

        //---------------------------------------------------------------------
        public ILogicListener getListener()
        {
            return mListener;
        }

        //---------------------------------------------------------------------
        public float getPumpingRate()
        {
            return mfPumpingRate;
        }

        //---------------------------------------------------------------------
        public bool isFishMustDie()
        {
            return mbFishMustDie;
        }

        //---------------------------------------------------------------------
        public bool isExistPlayer(uint et_player_rpcid)
        {
            if (mMapPlayerTurret.ContainsKey(et_player_rpcid))
            {
                return true;
            }
            else return false;
        }

        //-------------------------------------------------------------------------
        public void setTurret(uint et_player_rpcid, TbDataTurret.TurretType turret_type)
        {
            CLogicTurret turret = getTurret(et_player_rpcid);
            if (turret == null) return;
            turret.setTurret(turret_type);
        }

        //---------------------------------------------------------------------
        public Dictionary<uint, CLogicTurret> getMapTurret()
        {
            return mMapPlayerTurret;
        }

        //---------------------------------------------------------------------
        public CLogicTurret getTurret(uint et_player_rpcid)
        {
            if (mMapPlayerTurret.ContainsKey(et_player_rpcid))
            {
                return mMapPlayerTurret[et_player_rpcid];
            }
            else return null;
        }

        //---------------------------------------------------------------------
        public List<int> getListTurretRate()
        {
            return mListTurretRate;
        }

        //---------------------------------------------------------------------
        // ��ҽ�����̨
        public void scenePlayerEnter(uint et_player_rpcid, int turret_no, string nickname,
            bool is_bot, int default_turret_rate, TbDataTurret.TurretType turret_type)
        {
            scenePlayerLeave(et_player_rpcid);

            CLogicTurret turret = new CLogicTurret(this);
            turret.create(turret_no, et_player_rpcid, nickname, is_bot, default_turret_rate, turret_type);
            mMapPlayerTurret[et_player_rpcid] = turret;

            // ����˹㲥��ҽ���
            mProtocol.s2allcPlayerEnter(turret);
        }

        //---------------------------------------------------------------------
        // ����뿪��̨
        public void scenePlayerLeave(uint et_player_rpcid)
        {
            if (mMapPlayerTurret.ContainsKey(et_player_rpcid))
            {
                mMapPlayerTurret[et_player_rpcid].Dispose();
                mMapPlayerTurret.Remove(et_player_rpcid);

                // ����˹㲥����뿪
                mProtocol.s2allcPlayerLeave(et_player_rpcid);
            }
        }

        //---------------------------------------------------------------------
        // ��Ҹ��ʱ��
        public void scenePlayerRateChanged(uint et_player_rpcid, float player_rate)
        {
            if (mMapPlayerTurret.ContainsKey(et_player_rpcid))
            {
                mMapPlayerTurret[et_player_rpcid].playerRateChanged(player_rate);
            }
        }

        //---------------------------------------------------------------------
        // ������Ҹ��ʱ��
        public void scenePlayerRateChangedAll(List<float> list_rate)
        {
            int index = 0;
            foreach (var i in mMapPlayerTurret)
            {
                if (list_rate.Count > index)
                {
                    i.Value.playerRateChanged(list_rate[index]);
                }
                else
                {
                    i.Value.playerRateChanged(1.0f);
                }

                index++;
            }
        }

        //---------------------------------------------------------------------
        // ��ҵ���
        public void scenePlayerDropped(uint et_player_rpcid)
        {
            if (mMapPlayerTurret.ContainsKey(et_player_rpcid))
            {
                // ����˹㲥��ҵ���
                mProtocol.s2allcPlayerDropped(et_player_rpcid);
            }
            else
            {
                // log error
            }
        }

        //---------------------------------------------------------------------
        // �������
        public void scenePlayerReConnect(uint et_player_rpcid)
        {
            if (mMapPlayerTurret.ContainsKey(et_player_rpcid))
            {
                // ����˹㲥�������
                mProtocol.s2allcPlayerReConnect(et_player_rpcid);
            }
            else
            {
                // log error
            }
        }

        //---------------------------------------------------------------------
        public List<List<object>> addEffect(int vib_compose_id, Dictionary<string, object> param, EffectTypeEnum effect_type)
        {
            if (param != null)
            {
                param.Add("LogicScene", this);
                return mEffectMgr.createCombineEffect(vib_compose_id, param, effect_type);
            }
            else return null;
        }
    }
}
