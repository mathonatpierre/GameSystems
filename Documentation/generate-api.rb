#!/usr/bin/env ruby
# Generates a file:// friendly JavaScript API index directly from GameSystems C# sources.
require "json"

ROOT = File.expand_path("..", __dir__)
SOURCE_ROOT = File.join(ROOT, "Assets", "GameSystems")
OUTPUT = File.join(__dir__, "api-data.js")
VERSION_FILE = File.join(SOURCE_ROOT, "VERSION")

def line_number(source, offset)
  source[0...offset].count("\n") + 1
end

def matching_brace(source, opening)
  depth = 0
  state = :code
  i = opening
  while i < source.length
    char = source[i]
    following = source[i + 1]
    case state
    when :code
      if char == "/" && following == "/"
        state = :line_comment
        i += 1
      elsif char == "/" && following == "*"
        state = :block_comment
        i += 1
      elsif char == '"'
        state = :string
      elsif char == "'"
        state = :character
      elsif char == "{"
        depth += 1
      elsif char == "}"
        depth -= 1
        return i if depth.zero?
      end
    when :line_comment
      state = :code if char == "\n"
    when :block_comment
      if char == "*" && following == "/"
        state = :code
        i += 1
      end
    when :string
      if char == "\\"
        i += 1
      elsif char == '"'
        state = :code
      end
    when :character
      if char == "\\"
        i += 1
      elsif char == "'"
        state = :code
      end
    end
    i += 1
  end
  source.length - 1
end

def clean_signature(value)
  value.gsub(%r{//[^\n]*}, " ")
       .gsub(%r{/\*.*?\*/}m, " ")
       .gsub(/\s+/, " ")
       .gsub(/\s*([(),;{}])\s*/, '\\1')
       .strip
       .sub(/\s*\{\z/, "")
       .sub(/;\z/, "")
end

def xml_summary(source, offset)
  prefix = source[0...offset].lines.last(16)
  lines = []
  prefix.reverse_each do |line|
    stripped = line.strip
    break unless stripped.start_with?("///") || stripped.empty? || stripped.start_with?("[")
    lines.unshift(stripped.sub(%r{^///\s?}, "")) if stripped.start_with?("///")
  end
  text = lines.join(" ")
  summary = text[/<summary>(.*?)<\/summary>/m, 1] || text
  summary.gsub(/<[^>]+>/, "").gsub(/\s+/, " ").strip
end

def module_name(path)
  relative = path.sub(SOURCE_ROOT + File::SEPARATOR, "")
  relative.split(File::SEPARATOR).first
end

MODULE_ICONS = {
  "Abilities" => "abilities", "Characters" => "motor", "Core" => "core",
  "Feedbacks" => "feedbacks", "Hooks" => "hooks", "LevelGeneration" => "level-generation",
  "Playables" => "playables", "Sequencing" => "core", "Stats" => "stats"
}.freeze

def namespace_module(namespace)
  namespace.split(".")[1] || "Core"
end

def icon_name(type)
  name = type[:name]
  namespace = type[:namespace]
  return "ability-set" if name == "AbilitySet"
  return "abilities" if name == "CharacterAIDefinition"
  return "abilities" if name == "SequenceAbilityDefinition"
  return "playables" if %w[ProceduralAnimationClip AnimationClipAsset].include?(name)
  return "core" if name == "GameActionSequenceAsset"
  return "hooks" if name == "HookId"
  return "motor" if name.include?("Locomotion") || name.include?("Motor")
  return "effect" if name.include?("Reaction") || name.include?("Death") || name.include?("Respawn") || name.include?("Victory")
  return "level-generation" if name == "LevelGenerationProfile"
  return "platform-type" if name == "PlatformTypeDefinition"
  return "character-stats" if name == "CharacterStatsDefinition"
  return "formula" if name == "StatFormulaDefinition"
  return "stat" if name == "StatDefinition"
  return "attribute" if name == "AttributeDefinition"
  return "condition" if name.include?("Condition")
  return "effect" if name.include?("Effect")
  return "input-map" if name.include?("InputMap")
  return "feedback-sequence" if name == "FeedbackSequence"
  return "feedback-asset" if name == "FeedbackAsset"
  return "blend-1d" if name.include?("Blend1D")
  return "blend-2d" if name.include?("Blend2D")
  return "scene-hook" if name == "SceneHook"
  MODULE_ICONS.fetch(namespace_module(namespace), "core")
end

def declared_members(source, body_start, body_end, type_name, kind)
  return source[(body_start + 1)...body_end].split(",").map(&:strip).reject(&:empty?).map {
    |value| { kind: "enum value", name: value.split(/[\s=]/).first, signature: value, visibility: "public", line: line_number(source, body_start) }
  } if kind == "enum"

  members = []
  depth = 1
  start = body_start + 1
  i = start
  state = :code
  while i < body_end
    char = source[i]
    following = source[i + 1]
    case state
    when :code
      if char == "/" && following == "/"
        state = :line_comment
        i += 1
      elsif char == "/" && following == "*"
        state = :block_comment
        i += 1
      elsif char == '"'
        state = :string
      elsif char == "'"
        state = :character
      elsif char == "{"
        if depth == 1
          candidate = source[start..i]
          signature = clean_signature(candidate)
          unless signature.match?(/\b(class|struct|interface|enum)\b/)
            add_member(members, source, start, signature, type_name, true)
          end
        end
        depth += 1
      elsif char == "}"
        depth -= 1
        start = i + 1 if depth == 1
      elsif char == ";" && depth == 1
        signature = clean_signature(source[start..i])
        add_member(members, source, start, signature, type_name, false)
        start = i + 1
      end
    when :line_comment
      state = :code if char == "\n"
    when :block_comment
      if char == "*" && following == "/"
        state = :code
        i += 1
      end
    when :string
      if char == "\\" then i += 1 elsif char == '"' then state = :code end
    when :character
      if char == "\\" then i += 1 elsif char == "'" then state = :code end
    end
    i += 1
  end
  members.uniq { |member| [member[:kind], member[:signature]] }
end

def add_member(members, source, offset, signature, type_name, has_body)
  signature = signature.sub(/\A(?:\s*\[[^\]]+\]\s*)+/, "")
  return if signature.empty?
  visibility = signature[/\b(public|protected|internal)\b/, 1]
  return unless visibility
  return if signature.match?(/\b(class|struct|interface|enum)\b/)

  kind = if signature.include?(" event ") || signature.start_with?("event ")
           "event"
         elsif signature.include?("(")
           signature.match?(/\b#{Regexp.escape(type_name)}\s*\(/) ? "constructor" : "method"
         elsif has_body || signature.include?("=>")
           "property"
         else
           "field"
         end
  name = if %w[method constructor].include?(kind)
           signature[/([A-Za-z_][A-Za-z0-9_]*)\s*(?:<[^>]+>)?\s*\(/, 1]
         else
           signature.sub(/\s*=.*\z/, "").split.last&.sub(/\A.*\./, "")
         end
  return unless name
  members << {
    kind: kind,
    name: name,
    signature: signature,
    visibility: visibility,
    summary: xml_summary(source, offset),
    line: line_number(source, offset)
  }
end

types = []
Dir.glob(File.join(SOURCE_ROOT, "**", "*.cs")).sort.each do |path|
  source = File.read(path)
  namespace = source[/\bnamespace\s+([A-Za-z0-9_.]+)/, 1] || "Global"
  pattern = /(?<decl>\b(?<visibility>public|internal)\s+(?:(?:sealed|abstract|static|partial|readonly)\s+)*(?<kind>class|struct|interface|enum)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)(?<tail>[^\{;]*))\s*\{/m
  source.to_enum(:scan, pattern).each do
    match = Regexp.last_match
    opening = match.end(0) - 1
    closing = matching_brace(source, opening)
    declaration = clean_signature(match[:decl])
    relative = path.sub(ROOT + File::SEPARATOR, "")
    types << {
      module: module_name(path),
      namespace: namespace,
      name: match[:name],
      fullName: "#{namespace}.#{match[:name]}",
      kind: match[:kind],
      visibility: match[:visibility],
      declaration: declaration,
      summary: xml_summary(source, match.begin(0)),
      file: relative,
      line: line_number(source, match.begin(0)),
      editor: relative.include?("/Editor/"),
      members: declared_members(source, opening, closing, match[:name], match[:kind])
    }
  end
end

by_full_name = types.to_h { |type| [type[:fullName], type] }
by_short_name = types.group_by { |type| type[:name] }
base_name = lambda do |type|
  type[:declaration][/:\s*([A-Za-z_][A-Za-z0-9_.]*)/, 1]
end
inherits_from = lambda do |type, expected, visited = []|
  base = base_name.call(type)
  next false if base.nil? || visited.include?(type[:fullName])
  next true if base == expected || base.end_with?(".#{expected}")
  parent = by_full_name[base]
  parent ||= by_short_name[base]&.one? ? by_short_name[base].first : nil
  parent ? inherits_from.call(parent, expected, visited + [type[:fullName]]) : false
end

types.each do |type|
  type[:apiModule] = namespace_module(type[:namespace])
  type[:moduleIcon] = MODULE_ICONS.fetch(type[:apiModule], "core")
  type[:category] = if inherits_from.call(type, "ScriptableObject")
                      "Data"
                    elsif inherits_from.call(type, "MonoBehaviour")
                      "Runtime"
                    else
                      "Utilities & Interfaces"
                    end
  type[:icon] = icon_name(type)
end

types.sort_by! { |type| [type[:apiModule], type[:category], type[:namespace], type[:name]] }
payload = {
  version: File.exist?(VERSION_FILE) ? File.read(VERSION_FILE).strip : "dev",
  generatedAt: Time.now.strftime("%d/%m/%Y à %H:%M:%S %Z"),
  sourceRoot: "Assets/GameSystems",
  typeCount: types.length,
  memberCount: types.sum { |type| type[:members].length },
  types: types
}
File.write(OUTPUT, "window.GAME_SYSTEMS_API = #{JSON.generate(payload)};\n")
puts "Generated #{types.length} types and #{payload[:memberCount]} members in #{OUTPUT}"
