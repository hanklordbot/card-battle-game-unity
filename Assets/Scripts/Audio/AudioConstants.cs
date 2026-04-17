namespace CardBattle.Audio
{
    public static class AudioConstants
    {
        // BGM
        public const string BGM_MAIN_MENU = "bgm_main_menu";
        public const string BGM_BATTLE_NORMAL = "bgm_battle_normal";
        public const string BGM_BATTLE_TENSE = "bgm_battle_tense";
        public const string BGM_BATTLE_BOSS = "bgm_battle_boss";
        public const string BGM_DECK_EDIT = "bgm_deck_edit";
        public const string BGM_VICTORY = "bgm_victory";
        public const string BGM_DEFEAT = "bgm_defeat";

        // SFX — Card Ops
        public const string SFX_DRAW_CARD = "sfx_draw_card";
        public const string SFX_SET_CARD = "sfx_set_card";
        public const string SFX_FLIP_CARD = "sfx_flip_card";
        public const string SFX_CARD_SELECT = "sfx_card_select";
        public const string SFX_DISCARD = "sfx_discard";

        // SFX — Summon
        public const string SFX_NORMAL_SUMMON = "sfx_normal_summon";
        public const string SFX_TRIBUTE_SUMMON = "sfx_tribute_summon";
        public const string SFX_SPECIAL_SUMMON = "sfx_special_summon";
        public const string SFX_FUSION_SUMMON = "sfx_fusion_summon";
        public const string SFX_TRIBUTE_RELEASE = "sfx_tribute_release";

        // SFX — Battle
        public const string SFX_ATTACK_DECLARE = "sfx_attack_declare";
        public const string SFX_ATTACK_HIT = "sfx_attack_hit";
        public const string SFX_MONSTER_DESTROY = "sfx_monster_destroy";
        public const string SFX_DIRECT_ATTACK = "sfx_direct_attack";
        public const string SFX_DAMAGE_SMALL = "sfx_damage_small";
        public const string SFX_DAMAGE_LARGE = "sfx_damage_large";
        public const string SFX_ATTACK_REFLECT = "sfx_attack_reflect";

        // SFX — Spell/Trap
        public const string SFX_SPELL_ACTIVATE = "sfx_spell_activate";
        public const string SFX_TRAP_ACTIVATE = "sfx_trap_activate";
        public const string SFX_CHAIN_START = "sfx_chain_start";
        public const string SFX_CHAIN_STACK = "sfx_chain_stack";
        public const string SFX_CHAIN_RESOLVE = "sfx_chain_resolve";
        public const string SFX_NEGATE = "sfx_negate";

        // SFX — Turn
        public const string SFX_TURN_START_MINE = "sfx_turn_start_mine";
        public const string SFX_TURN_START_OPPONENT = "sfx_turn_start_opponent";
        public const string SFX_PHASE_CHANGE = "sfx_phase_change";
        public const string SFX_TURN_END = "sfx_turn_end";

        // SFX — LP
        public const string SFX_LP_DAMAGE = "sfx_lp_damage";
        public const string SFX_LP_HEAL = "sfx_lp_heal";
        public const string SFX_LP_WARNING = "sfx_lp_warning";

        // SFX — Result
        public const string SFX_VICTORY = "sfx_victory";
        public const string SFX_DEFEAT = "sfx_defeat";

        // SFX — UI
        public const string SFX_UI_CLICK = "sfx_ui_click";
        public const string SFX_UI_HOVER = "sfx_ui_hover";
        public const string SFX_UI_POPUP_OPEN = "sfx_ui_popup_open";
        public const string SFX_UI_POPUP_CLOSE = "sfx_ui_popup_close";
        public const string SFX_MATCH_FOUND = "sfx_match_found";
        public const string SFX_COUNTDOWN_TICK = "sfx_countdown_tick";

        // Timing
        public const float CROSSFADE_DURATION = 2.0f;
        public const float BOSS_MIN_DURATION = 30f;
        public const float RESULT_SFX_DELAY = 1.0f;
    }
}
