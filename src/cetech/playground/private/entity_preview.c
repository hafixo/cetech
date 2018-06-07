//==============================================================================
// Include
//==============================================================================

#include "cetech/kernel/memory/allocator.h"

#include "cetech/kernel/hashlib/hashlib.h"
#include "cetech/kernel/memory/memory.h"
#include "cetech/kernel/api/api_system.h"
#include "cetech/kernel/log/log.h"
#include <cetech/kernel/module/module.h>
#include <cetech/playground/entity_property.h>
#include <cetech/kernel/cdb/cdb.h>
#include <cetech/engine/ecs/ecs.h>
#include <cetech/engine/transform/transform.h>
#include <cetech/playground/asset_preview.h>

//==============================================================================
// GLobals
//==============================================================================

#define _G EntityPreviewGlobals
static struct _G {
    struct ct_entity ent;
} _G;

CETECH_DECL_API(ct_memory_a0);
CETECH_DECL_API(ct_transform_a0);
CETECH_DECL_API(ct_hashlib_a0);
CETECH_DECL_API(ct_asset_preview_a0);
CETECH_DECL_API(ct_ecs_a0);

static void load(const char *filename,
                 struct ct_resource_id resourceid,
                 struct ct_world world) {
    struct ct_entity ent = ct_ecs_a0.spawn_entity(world, resourceid.name);
    _G.ent = ent;
}

static void unload(const char *filename,
                   struct ct_resource_id resourceid,
                   struct ct_world world) {
    ct_ecs_a0.destroy_entity(world, &_G.ent, 1);
}


static int _init(struct ct_api_a0 *api) {
    CT_UNUSED(api);

    _G = (struct _G) {};


    ct_asset_preview_a0.register_type_preview(
            "entity",
            (struct ct_asset_preview_fce) {
                    .load = load,
                    .unload = unload
            });


    return 1;
}

static void _shutdown() {
    ct_asset_preview_a0.unregister_type_preview("entity");

    _G = (struct _G) {};
}


CETECH_MODULE_DEF(
        entity_preview,
        {
            CETECH_GET_API(api, ct_memory_a0);
            CETECH_GET_API(api, ct_transform_a0);
            CETECH_GET_API(api, ct_hashlib_a0);
            CETECH_GET_API(api, ct_asset_preview_a0);
            CETECH_GET_API(api, ct_ecs_a0);

        },
        {
            CT_UNUSED(reload);
            _init(api);
        },
        {
            CT_UNUSED(reload);
            CT_UNUSED(api);

            _shutdown();
        }
)