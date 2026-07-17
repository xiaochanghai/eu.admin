# Extensible FormPage Design

## Goal

Extend `src/components/Elements/FormPage.tsx` so business modules can reuse its loading, saving, readonly, and layout behavior while keeping module-specific field interactions and extra content in a thin wrapper. The first supported module is the user form. Existing group/company cascading and immediate role persistence must remain.

## Architecture

`FormPage` keeps ownership of module metadata, record querying, form state, saving, readonly state, toolbar state, and page/modal/drawer layout. Business wrappers receive a render context through three narrow extension points:

- `onQuerySuccess(data, context)` synchronizes business state after loading a record.
- `renderField(field, defaultElement, context)` overrides an individual field.
- `renderExtraContent(context)` adds content after the main form.

The user wrapper owns only `groupId` and the user-role UI. Role fetching and immediate persistence move into a focused `UserRoleTree` component.

## Extension Context

The context exposes the Ant Design form instance, current persisted `id`, effective disabled/readonly state, `modifyType`, and `moduleInfo`. With no extension props, existing generic form behavior remains unchanged.

## User Behavior

The user wrapper delegates record querying and saving to `FormPage`. Its field renderer clears `CompanyId` when `GroupId` changes, hides `CompanyId` without a selected group, and supplies the selected group through `parentColumn` and `parentId`.

After a record loads, `onQuerySuccess` initializes `groupId`. Extra content is hidden until a persisted user ID exists. `UserRoleTree` then loads roles and checked keys. Every check immediately calls `BatchInsertUserRole`. View mode disables the tree and prevents mutation.

## Error and Concurrency Handling

The query callback runs after form values are populated. Role loading remains independent of main-form loading. During a role update, the tree is disabled to prevent overlapping writes; a failed write restores the previous checked keys.

## Verification

- Type-check and lint changed files.
- Confirm generic modules render unchanged without extensions.
- Confirm user add/edit continues through existing modal or drawer controls.
- Confirm changing group clears and filters company.
- Confirm roles appear only after a user ID exists and persist immediately.
- Confirm view mode disables form and role mutation.
