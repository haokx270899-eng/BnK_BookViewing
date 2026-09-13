type Props = { value: string; onChange: (date: string) => void };
export function DateSelector({ value, onChange }: Props) {
  return (
    <label>
      Date
      <input
        type="date"
        value={value}
        onChange={(event) => onChange(event.target.value)}
      />
    </label>
  );
}
