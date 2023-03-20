using System.Numerics;

namespace StudioCore
{
    /*
        I spelled it with 'u' because this is a joke
    */
    public class Style
    {
        public static Style Dark { get; private set; } = new();
        //public static Style Light { get; private set; } = new();
        public static Style Current { get; private set; } = Dark;

        // General settings

        public Vector4 REAL_BG_COLOUR = new (0.0f, 0.0f, 0.0f, 0.0f);

        public Vector4 SETTINGS_BG_COLOUR = new (0f, 0f, 0f, 0.98f);
        public Vector4 SETTINGS_TITLE_COLOUR = new (0.25f, 0.25f, 0.25f, 1.0f);
        public Vector4 SETTINGS_HEADER_COLOUR = new (0.3f, 0.3f, 0.6f, 0.4f);

        public Vector4 PRIMARY_BG_COLOUR = new (0.176f, 0.176f, 0.188f, 1.0f);
        public Vector4 SECONDARY_BG_COLOUR = new (0.176f, 0.176f, 0.188f, 1.0f);
        public Vector4 HIGHLIGHT_BG_COLOUR = new (0.106f, 0.106f, 0.110f, 1.0f);
        public Vector4 BORDER_COLOUR = new (0.247f, 0.247f, 0.275f, 1.0f);
        public Vector4 FRAME_BG_COLOUR = new (0.200f, 0.200f, 0.216f, 1.0f);
        public Vector4 ELEMENT_HOVERED_COLOUR = new (0.247f, 0.247f, 0.275f, 1.0f);
        public Vector4 ELEMENT_ACTIVE_COLOUR = new (0.200f, 0.600f, 1.000f, 1.0f);
        public Vector4 SCROLLBAR_BG_COLOUR = new (0.243f, 0.243f, 0.249f, 1.0f);
        public Vector4 SCROLLBAR_GRAB_COLOUR = new (0.408f, 0.408f, 0.408f, 1.0f);
        public Vector4 SLIDE_GRAB_COLOUR = new (0.635f, 0.635f, 0.635f, 1.0f);
        public Vector4 SLIDE_ACTIVE_COLOUR = new (1.000f, 1.000f, 1.000f, 1.0f);
        public Vector4 HEADER_COLOUR = new (0.000f, 0.478f, 0.800f, 1.0f);
        public Vector4 HEADER_ACTIVE_COLOUR = new (0.161f, 0.550f, 0.939f, 1.0f);
        public Vector4 TAB_COLOUR = new (0.176f, 0.176f, 0.188f, 1.0f);
        public Vector4 TAB_HOVERED_COLOUR = new (0.110f, 0.592f, 0.918f, 1.0f);
        public Vector4 CHECKMARK_COLOUR = new (1.000f, 1.000f, 1.000f, 1.0f);

        // MSB Editor misc
        public Vector4 MSB_BG_COLOUR = new Vector4(0.145f, 0.145f, 0.149f, 1.0f);
        public Vector4 MSB_CHALICE_ERROR_BG_COLOUR = new (0.8f, 0.2f, 0.2f, 1.0f);

        // DisplayGroup window
        public Vector4 DRAW_DISP_BG_COLOUR = new (0.4f, 0.4f, 0.06f, 1.0f);
        public Vector4 DRAW_DISP_CHECK_COLOUR = new (1f, 1f, 0.02f, 1.0f);
        public Vector4 DRAW_BG_COLOUR = new (0.02f, 0.3f, 0.02f, 1.0f);
        public Vector4 DRAW_CHECK_COLOUR = new (0.2f, 1.0f, 0.2f, 1.0f);
        public Vector4 DISP_BG_COLOUR = new (0.4f, 0.06f, 0.06f, 1.0f);
        public Vector4 DISP_CHECK_COLOUR = new (1.0f, 0.2f, 0.2f, 1.0f);

        // MSB multi-edit
        public Vector4 MSB_PROPEDIT_MULTIOBJECT_COLOUR = new (0.5f, 1.0f, 0.0f, 1.0f);
        public Vector4 MSB_PROPEDIT_MULTIOBJECT_BG_COLOUR = new (0.0f, 0.5f, 0.0f, 0.1f);

        // Param PropEditor
        public Vector4 PARAM_MODIFIED_BG_COLOUR = new (0.2f, 0.22f, 0.2f, 1.0f);
        public Vector4 PARAM_CONFLICT_BG_COLOUR = new (0.25f, 0.2f, 0.2f, 1.0f);
        public Vector4 PARAM_MERGEABLE_BG_COLOUR = new (0.2f, 0.2f, 0.236f, 1.0f);
        public Vector4 REFERENCE_ANY_DATA_COLOUR = new (1.0f, 0.5f, 1.0f, 1.0f);
        public Vector4 PARAM_DEFAULT_DATA_COLOUR = new (0.75f, 0.75f, 0.75f, 1.0f);

        public Vector4 PARAM_AUX_BG_COLOUR = new (0.180f, 0.180f, 0.196f, 1.0f);
        public Vector4 PARAM_AUX_COLOUR = new (0.9f, 0.9f, 0.9f, 1.0f);

        // Text general colouration
        public Vector4 UPDATE_COLOUR = new (0.0f, 1.0f, 0.0f, 1.0f);
        public Vector4 WARNING_COLOUR = new (1.0f, 0f, 0f, 1.0f);
        public Vector4 WARNING_HINT_COLOUR = new (1.0f, 1.0f, 1.0f, 1.0f);
        public Vector4 HINT_ICON_COLOUR = new (0.6f, 0.6f, 1.0f, 1.0f);
        public Vector4 MSB_DUPLICATION_HINT_COLOUR = new (1.0f, 1.0f, 1.0f, 0.5f);
        public Vector4 MSB_EDITOR_PARAM_HEADER_COLOUR = new (0.8f, 0.8f, 0.8f, 1.0f);
        public Vector4 PARAM_EDITOR_PARAM_HEADER_COLOUR = new (0.8f, 0.8f, 1.0f, 1.0f);
        public Vector4 PARAM_HOTRELOAD_DISCLAIM_COLOUR = new (1.0f, 1.0f, 0.0f, 1.0f);
        public Vector4 PARAM_UPGRADE_COLOUR = new (0.0f, 1f, 0f, 1.0f);
        public Vector4 PARAM_TRUE_NAME_COLOUR = new (1f, 0.7f, 0.4f, 1.0f);
        public Vector4 PARAM_FMG_COLOUR = new (1.0f, 1.0f, 0.0f, 1.0f);
        public Vector4 TEXT_WARNING_COLOUR = new (1.0f, 0.0f, 0.0f, 1.0f);

        // Text type colouration
        public Vector4 REFERENCE_PARAM_LABEL_COLOUR = new (1.0f, 1.0f, 0.0f, 1.0f);
        public Vector4 REFERENCE_PARAM_LABEL_DISABLED_COLOUR = new (0.7f, 0.7f, 0.7f, 1.0f);
        public Vector4 REFERENCE_FMG_LABEL_COLOUR = new (1.0f, 1.0f, 0.0f, 1.0f);
        public Vector4 REFERENCE_ENUM_LABEL_COLOUR = new (1.0f, 1.0f, 0.0f, 1.0f);
        public Vector4 REFERENCE_PARAM_VALUE_COLOUR = new (1.0f, 0.5f, 0.5f, 1.0f);
        public Vector4 REFERENCE_PARAM_VALUE_INVALID_COLOUR = new (0.0f, 0.0f, 0.0f, 1.0f);
        public Vector4 REFERENCE_FMG_VALUE_COLOUR = new (1.0f, 0.5f, 0.5f, 1.0f);
        public Vector4 REFERENCE_ENUM_VALUE_COLOUR = new (1.0f, 0.5f, 0.5f, 1.0f);

        // Text state colouration
        public Vector4 MSB_ITEM_VISIBLE_COLOUR = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        public Vector4 MSB_ITEM_INVISIBLE_COLOUR = new Vector4(0.6f, 0.6f, 0.6f, 1.0f);
        public Vector4 MSB_MAP_ALIAS_COLOUR = new (1.0f, 1.0f, 0.0f, 1.0f);
        public Vector4 MSB_MAP_ALIAS_UNUSED_COLOUR = new (1.0f, 0.0f, 0.0f, 1.0f);
        public Vector4 PARAM_PRIMARY_MODIFIED_COLOUR = new Vector4(0.7f, 1.0f, 0.7f, 1.0f);
        public Vector4 PARAM_PRIMARY_UNMODIFIED_COLOUR = new Vector4(0.9f,0.9f,0.9f,1);
        public Vector4 PARAM_AUX_ADDED_COLOUR = new Vector4(0.7f, 0.7f, 1.0f, 1.0f);
        public Vector4 PARAM_AUX_CONFLICT_COLOUR = new Vector4(1.0f, 0.7f, 0.7f, 1.0f);

    }
}
