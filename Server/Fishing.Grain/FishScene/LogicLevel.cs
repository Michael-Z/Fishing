using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using GF.Unity.Common;

namespace Fishing
{
    public class CLogicLevel : EbFsm, IDisposable
    {
        //---------------------------------------------------------------------
        CLogicScene mScene = null;// ����ָ��
        TbDataLevel mVibLevel = null;// �ؿ�vib
        float mLevelRate = 1.0f;
        public int CurMapVibId { get; set; }
        public int NextMapVibId { get; set; }
        System.Random mRandom = new System.Random(unchecked((int)System.DateTime.Now.Ticks));
        System.Random mScoreRandom = new System.Random(unchecked((int)System.DateTime.Now.Ticks));// ���������н������ڼ����������
        float mTimeFactor = 1.0f;// ʱ������
        bool mPauseCreateFishCrowd = false;// ��ͣ����
        BaseFishLordMgr mBaseFishLordMgr = null;
        LogicParticleSystemOuter mLogicParticleSystemOuter = null;

        //---------------------------------------------------------------------
        public CLogicLevel(CLogicScene logic_scene)
        {
            mScene = logic_scene;
        }

        //---------------------------------------------------------------------
        ~CLogicLevel()
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
        public void create(int level_vibid, float level_rate, List<JsonPacket> json_packet_list, List<RouteJsonPacket> route_json_packet_list)
        {
            mVibLevel = EbDataMgr.Instance.getData<TbDataLevel>(level_vibid);
            mLevelRate = level_rate;

            _initBaseFishLordMgr(json_packet_list, route_json_packet_list);

            addState(new CLogicLevelStateNormal(mScene, this));
            addState(new CLogicLevelStateSwitch(mScene, this));

            setupFsm();
        }

        //---------------------------------------------------------------------
        public void destroy()
        {
            destroyFsm();

            if (mLogicParticleSystemOuter != null)
            {
                mLogicParticleSystemOuter.destroy();
                mLogicParticleSystemOuter = null;
            }

            if (mBaseFishLordMgr != null)
            {
                mBaseFishLordMgr.destroy();
                mBaseFishLordMgr = null;
            }
        }

        //---------------------------------------------------------------------
        public void update(float elapsed_tm)
        {
            processEvent("update", elapsed_tm);

            mBaseFishLordMgr.update(elapsed_tm * mTimeFactor);
        }

        //---------------------------------------------------------------------
        public void updateOutFish(float elapsed_tm)
        {
            mLogicParticleSystemOuter.update(elapsed_tm * mTimeFactor);
        }

        //---------------------------------------------------------------------
        public bool isFormation()
        {
            return mLogicParticleSystemOuter.isFormation();
        }

        //---------------------------------------------------------------------
        // ����������м���
        public void c2sFishHit(uint et_player_rpcid, int bullet_objid, int fish_objid)
        {
            CLogicFish fish = (CLogicFish)mBaseFishLordMgr.getBaseEntity(fish_objid);
            if (fish == null)
            {
                return;
            }
            CLogicTurret turret = mScene.getTurret(et_player_rpcid);
            if (turret == null) return;

            CLogicBullet bullet = turret.getBullet(bullet_objid);
            if (bullet == null) return;

            // ֪ͨ����ģ��������
            mScene.getListener().onLogicSceneFishHit(et_player_rpcid, fish.FishVibId, bullet.getRate());

            _calculateFishScore(et_player_rpcid, turret, fish, bullet.getRate(), bullet_objid);

            turret.removeBullet(bullet_objid);
        }

        //---------------------------------------------------------------------
        // ��������������м���
        public void c2sFishNetHit(uint et_player_rpcid, int fish_objid)
        {
            CLogicFish fish = (CLogicFish)mBaseFishLordMgr.getBaseEntity(fish_objid);
            if (fish == null) return;

            CLogicTurret turret = mScene.getTurret(et_player_rpcid);
            if (turret == null) return;

            _calculateFishScore(et_player_rpcid, turret, fish, turret.getTurretRate(), -1);
        }

        //---------------------------------------------------------------------
        // ������������ֵ���������״̬
        void _calculateFishScore(uint et_player_rpcid, CLogicTurret turret, CLogicFish fish, int rate, int bullet_objid)
        {
            // ��������������ʣ�������������
            int score = 1;// ��ֵ
            int effect_fish_vib_id = -1;

            bool fish_die = fish.hit(et_player_rpcid, rate, ref score, ref effect_fish_vib_id);
            if (fish_die)
            {
                // �������
                if (turret.getBufferPower()) rate *= 2;
                int total_score = rate * score;

                _tScenePlayer player = turret.getScenePlayerInfo();
                int cur_gold = mScene.getListener().onLogicSceneGetPlayerGold(player.et_player_rpcid);
                cur_gold += total_score;
                mScene.getListener().onLogicSceneSetPlayerGold(player.et_player_rpcid, cur_gold, fish.FishVibId, "FishLord", rate);

                // ֪ͨ����ģ��������
                mScene.getListener().onLogicSceneFishDie(et_player_rpcid, fish.FishVibId, total_score);

                // ����˹㲥������
                mScene.getProtocol().s2allcFishDie(et_player_rpcid,
                    total_score, bullet_objid, fish.FishObjId, effect_fish_vib_id, rate);
            }
        }

        //---------------------------------------------------------------------
        public _eLevelState getLevelState()
        {
            List<EbState> list_state = getCurrentStateList();
            if (list_state.Count > 1)
            {
                EbState state = list_state[1];
                if (state._getStateName() == "CLogicLevelStateNormal")
                {
                    return _eLevelState.Normal;
                }
                else if (state._getStateName() == "CLogicLevelStateSwitch")
                {
                    return _eLevelState.Switch;
                }
            }

            return _eLevelState.Normal;
        }

        //---------------------------------------------------------------------
        public void outFormation()
        {
            mLogicParticleSystemOuter.outFormation();
        }

        //---------------------------------------------------------------------
        public int getLevelVibId()
        {
            return mVibLevel.Id;
        }

        //---------------------------------------------------------------------
        public float getCurSecond()
        {
            List<EbState> list_state = getCurrentStateList();
            if (list_state.Count > 1)
            {
                EbState state = list_state[1];
                if (state._getStateName() == "CLogicLevelStateNormal")
                {
                    CLogicLevelStateNormal s = (CLogicLevelStateNormal)state;
                    return s.getCurSecond();
                }
                else if (state._getStateName() == "CLogicLevelStateSwitch")
                {
                    CLogicLevelStateSwitch s = (CLogicLevelStateSwitch)state;
                    return s.getCurSecond();
                }
            }

            return 0.0f;
        }

        //---------------------------------------------------------------------
        public float getMaxSecond()
        {
            List<EbState> list_state = getCurrentStateList();
            if (list_state.Count > 1)
            {
                EbState state = list_state[1];
                if (state._getStateName() == "CLogicLevelStateNormal")
                {
                    CLogicLevelStateNormal s = (CLogicLevelStateNormal)state;
                    return s.getMaxSecond();
                }
                else if (state._getStateName() == "CLogicLevelStateSwitch")
                {
                    CLogicLevelStateSwitch s = (CLogicLevelStateSwitch)state;
                    return s.getMaxSecond();
                }
            }

            return 0.0f;
        }

        //---------------------------------------------------------------------
        public void outRedFish(int normal_fish_vib_id, int red_fish_vib_id, int fish_count, EbVector3 position, int red_fish_obj_id)
        {
            mLogicParticleSystemOuter.outRedFish(normal_fish_vib_id, red_fish_vib_id, fish_count, position, red_fish_obj_id);
        }

        //---------------------------------------------------------------------
        public int getRandoNumber(int min, int max)// �����䣬��������С�����ֵ
        {
            if (min > max)
            {
                return min;
            }
            return mRandom.Next(min, max + 1);
        }

        //---------------------------------------------------------------------
        public double getRandomScore()// �����䣬��������С�����ֵ
        {
            return mScoreRandom.NextDouble();
        }

        //---------------------------------------------------------------------
        // ����ʱ������
        public void setTimeFactor(float factor)
        {
            mTimeFactor = factor;
            if (mTimeFactor > 1.0f)
            {
                mTimeFactor = 1.0f;
            }
            if (mTimeFactor < 0.0f)
            {
                mTimeFactor = 0.0f;
            }
        }

        //---------------------------------------------------------------------
        // ��ȡʱ������
        public float getTimeFactor()
        {
            return mTimeFactor;
        }

        //---------------------------------------------------------------------
        // �����Ƿ���ͣ����
        public void setPauseCreateFishCrowd(bool pause)
        {
            mPauseCreateFishCrowd = pause;
        }

        //---------------------------------------------------------------------
        // ��ȡ�Ƿ���ͣ����
        public bool getPauseCreateFishCrowd()
        {
            return mPauseCreateFishCrowd;
        }

        //---------------------------------------------------------------------
        public List<CLogicFish> getListFishById(int fish_vib_id)
        {
            List<BaseEntity> entity_list = mBaseFishLordMgr.getEntityListById(fish_vib_id);

            List<CLogicFish> list_fish = new List<CLogicFish>();
            foreach (var entity in entity_list)
            {
                list_fish.Add((CLogicFish)entity);
            }
            return list_fish;
        }

        //---------------------------------------------------------------------
        public List<CLogicFish> getAllFish()
        {
            List<BaseEntity> entity_list = mBaseFishLordMgr.getAllEntity();

            List<CLogicFish> list_fish = new List<CLogicFish>();
            foreach (var entity in entity_list)
            {
                list_fish.Add((CLogicFish)entity);
            }
            return list_fish;
        }

        //---------------------------------------------------------------------
        public CLogicFish findFish(int fish_objid)
        {
            return (CLogicFish)mBaseFishLordMgr.findBaseEntity(fish_objid);
        }

        //---------------------------------------------------------------------
        public void clearAllFish()
        {
            mLogicParticleSystemOuter.clearAllFish();
            mBaseFishLordMgr.clearAllEntity();
        }

        //---------------------------------------------------------------------
        public int genRandomMap()
        {
            Dictionary<int, EbData> mapData = EbDataMgr.Instance.getMapData<TbDataMap>();
            Dictionary<int, TbDataMap> map_datas = new Dictionary<int, TbDataMap>();
            foreach (var it in mapData)
            {
                map_datas[it.Key] = (TbDataMap)it.Value;
            }
            List<int> map_id_list = new List<int>();
            foreach (var it in map_datas)
            {
                if (it.Value.Id > 0)
                {
                    map_id_list.Add(it.Key);
                }
            }

            System.Random rd = new System.Random(unchecked((int)System.DateTime.Now.Ticks));
            int rand_index = rd.Next(0, map_id_list.Count);
            int id = map_id_list[rand_index];
            return id;
        }

        //---------------------------------------------------------------------
        void _initBaseFishLordMgr(List<JsonPacket> json_packet_list, List<RouteJsonPacket> route_json_packet_list)
        {
            ParticleSystemFactory fish_lord_factory = new ParticleSystemFactory();

            // ����������
            fish_lord_factory.regGeneratorFactory(new DivergencePointGeneratorFactory());
            fish_lord_factory.regGeneratorFactory(new LineGeneratorFactory());
            fish_lord_factory.regGeneratorFactory(new CircleGeneratorFactory());
            fish_lord_factory.regGeneratorFactory(new IntervalDivergenceGeneratorFactory());
            fish_lord_factory.regGeneratorFactory(new RoundCircleGeneratorFactory());
            fish_lord_factory.regGeneratorFactory(new NormalGeneratorFactory());
            fish_lord_factory.regGeneratorFactory(new RedFishGeneratorFactory());
            fish_lord_factory.regGeneratorFactory(new FixGeneratorFactory());

            // Ӱ��������
            fish_lord_factory.regAffectorFactory(new StopAffectorFactory());
            fish_lord_factory.regAffectorFactory(new StartMoveAffectorFactory());
            fish_lord_factory.regAffectorFactory(new AngleSpeedAffectorFactory());

            mBaseFishLordMgr = new BaseFishLordMgr(new LogicFishFactory(mScene), fish_lord_factory);
            mBaseFishLordMgr.create(json_packet_list, route_json_packet_list);

            mLogicParticleSystemOuter = new LogicParticleSystemOuter();
            mLogicParticleSystemOuter.create(mScene, mBaseFishLordMgr);
        }
    }
}
