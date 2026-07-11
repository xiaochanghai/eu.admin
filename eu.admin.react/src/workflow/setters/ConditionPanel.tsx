import { memo, useCallback, useMemo } from "react";
import QueryBuilder from "@/components/queryBuilder";
import { FormVo } from "@/api/Form";
import { ConditionGroup } from "@/dsl/base";

export const ConditionPanel = memo(
  (props: { value?: ConditionGroup[]; formVo?: FormVo; onChange: (value?: ConditionGroup[]) => void }) => {
    const { formVo, value, onChange } = props;
    const builderValue = useMemo(() => JSON.stringify(value?.length ? value : [{ where: [{}] }]), [value]);

    const handleDataChange = useCallback(
      (conditionJson: string) => {
        try {
          const conditions = JSON.parse(conditionJson);
          if (Array.isArray(conditions)) onChange(conditions as ConditionGroup[]);
        } catch {
          // Ignore malformed intermediate input and preserve the saved conditions.
        }
      },
      [onChange]
    );
    return (
      <>
        {formVo && (
          <QueryBuilder
            entityModel={formVo}
            value={builderValue}
            onDataChange={handleDataChange}
          />
        )}
      </>
    );
  }
);
