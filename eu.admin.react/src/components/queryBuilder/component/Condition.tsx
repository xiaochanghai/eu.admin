import { Input, InputNumber, Select } from "antd";
import { useMemo } from "react";
import { FormVo } from "@/api/Form";
import { OptEnum, where } from "@/dsl/base";

interface ConditionProps {
  where: Partial<where>;
  formVo: FormVo;
  className?: string;
  onDataChange: (where: Partial<where>) => void;
}

const operators: Record<string, Array<{ value: OptEnum; label: string }>> = {
  string: [
    { value: OptEnum.eq, label: "Equals" },
    { value: OptEnum.ne, label: "Not equals" },
    { value: OptEnum.like, label: "Contains" },
    { value: OptEnum.startsWith, label: "Starts with" },
    { value: OptEnum.endsWith, label: "Ends with" },
    { value: OptEnum.isNull, label: "Is empty" },
    { value: OptEnum.isNotNull, label: "Is not empty" }
  ],
  number: [
    { value: OptEnum.eq, label: "Equals" },
    { value: OptEnum.ne, label: "Not equals" },
    { value: OptEnum.gt, label: ">" },
    { value: OptEnum.goe, label: ">=" },
    { value: OptEnum.lt, label: "<" },
    { value: OptEnum.loe, label: "<=" },
    { value: OptEnum.isNull, label: "Is empty" },
    { value: OptEnum.isNotNull, label: "Is not empty" }
  ],
  date: [
    { value: OptEnum.eq, label: "Equals" },
    { value: OptEnum.gt, label: "After" },
    { value: OptEnum.goe, label: "On or after" },
    { value: OptEnum.lt, label: "Before" },
    { value: OptEnum.loe, label: "On or before" },
    { value: OptEnum.isNull, label: "Is empty" },
    { value: OptEnum.isNotNull, label: "Is not empty" }
  ],
  boolean: [
    { value: OptEnum.eq, label: "Equals" },
    { value: OptEnum.ne, label: "Not equals" }
  ]
};

const Condition = ({ where, formVo, className, onDataChange }: ConditionProps) => {
  const field = useMemo(() => formVo.fields.find(item => item.fieldName === where.fieldName), [formVo.fields, where.fieldName]);
  const fieldType = field?.fieldType ?? "string";
  const options = operators[fieldType] ?? operators.string;
  const needsValue = where.opt !== OptEnum.isNull && where.opt !== OptEnum.isNotNull;

  return (
    <div className={className} style={{ display: "flex", gap: 8 }}>
      <Select
        style={{ width: 150 }}
        placeholder="Field"
        value={where.fieldName}
        options={formVo.fields.map(item => ({ value: item.fieldName, label: item.title || item.fieldName }))}
        onChange={fieldName => {
          const selected = formVo.fields.find(item => item.fieldName === fieldName);
          const selectedType = selected?.fieldType ?? "string";
          onDataChange({
            fieldName,
            entityName: formVo.entityType,
            fieldType: selectedType,
            opt: operators[selectedType]?.[0]?.value ?? OptEnum.eq,
            value: undefined,
            desc: { fieldName: selected?.title }
          });
        }}
      />
      <Select
        style={{ width: 140 }}
        placeholder="Operator"
        value={where.opt}
        options={options}
        onChange={opt => onDataChange({ ...where, opt, value: opt === OptEnum.isNull || opt === OptEnum.isNotNull ? undefined : where.value })}
      />
      {field && needsValue && fieldType === "number" && (
        <InputNumber style={{ width: 150 }} placeholder="Value" value={where.value?.[0]} onChange={value => onDataChange({ ...where, value: value === null ? undefined : [value] })} />
      )}
      {field && needsValue && fieldType !== "number" && (
        <Input style={{ width: 180 }} placeholder="Value" value={where.value?.[0]} onChange={event => onDataChange({ ...where, value: [event.target.value] })} />
      )}
    </div>
  );
};

export default Condition;
