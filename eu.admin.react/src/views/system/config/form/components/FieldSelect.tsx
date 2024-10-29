interface FieldSelectProps {
  className?: string;
  fields: any[]; //参与排序的字段
  currentField?: string; //外部选中字段
  onDataChange: (ang: any[]) => void; //数据返回出去
  onSelect: (field: string) => void; //当前选中字段
}
// const FieldSelect = ({ fields, mode, onSelect, className, onDataChange }: FieldSelectProps) => {
const FieldSelect = ({ className }: FieldSelectProps) => {
  return <div className={`${className}  bottom-1 border-black p-4`}></div>;
};

export default FieldSelect;
