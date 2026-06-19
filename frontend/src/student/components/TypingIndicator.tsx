export default function TypingIndicator() {
  return (
    <div className="flex gap-3 mb-4 items-end">
      <div className="w-9 h-9 rounded-full bg-gradient-to-br from-brand-400 to-purple-600 flex items-center justify-center text-lg shrink-0 shadow-md">
        🤖
      </div>
      <div className="bg-gradient-to-r from-brand-50 to-purple-50 border-2 border-brand-200 rounded-3xl rounded-bl-sm px-5 py-3 shadow-sm">
        <div className="flex items-center gap-2">
          <span className="text-sm font-display font-bold text-brand-600">Thinking</span>
          <div className="flex gap-1 items-center">
            <span className="w-2 h-2 bg-brand-400 rounded-full animate-bounce" />
            <span className="w-2 h-2 bg-brand-400 rounded-full animate-bounce bounce-delay-1" />
            <span className="w-2 h-2 bg-brand-400 rounded-full animate-bounce bounce-delay-2" />
          </div>
          <span className="text-base animate-spin">✨</span>
        </div>
      </div>
    </div>
  )
}
