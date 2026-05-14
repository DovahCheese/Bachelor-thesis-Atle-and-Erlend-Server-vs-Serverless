/**
 * Fetches all 5-letter Norwegian Bokmål words from ordbokapi.org
 * and writes them to src/WebApi/Data/words.txt.
 *
 * Usage:  node scripts/fetch-words.mjs
 *
 * Words are sourced from Bokmålsordboka via the ordbokapi GraphQL API
 * by iterating through all article IDs. Only single, 5-letter words
 * composed entirely of Norwegian letters (a-z + æøå) are kept.
 */

import { writeFileSync } from 'fs'
import { resolve, dirname } from 'path'
import { fileURLToPath } from 'url'

const __dir = dirname(fileURLToPath(import.meta.url))
const OUT = resolve(__dir, '../src/WebApi/Data/words.txt')
const API = 'https://api.ordbokapi.org/graphql'
const MAX_ID = 70000
const BATCH = 50          // concurrent requests per round
const DELAY_MS = 200      // pause between batches (be polite to the API)

const VALID = /^[a-zæøå]{5}$/

async function fetchArticle(id) {
  const body = JSON.stringify({
    query: `{ article(id: ${id}, dictionary: Bokmaalsordboka) { lemmas { lemma paradigms { inflections { wordForm } } } } }`
  })
  const res = await fetch(API, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body
  })
  const json = await res.json()
  const lemmas = json?.data?.article?.lemmas ?? []
  const words = []
  for (const lemma of lemmas) {
    words.push(lemma.lemma.toLowerCase())
    for (const paradigm of lemma.paradigms ?? []) {
      for (const inflection of paradigm.inflections ?? []) {
        if (inflection.wordForm) words.push(inflection.wordForm.toLowerCase())
      }
    }
  }
  return words
}

async function sleep(ms) {
  return new Promise(r => setTimeout(r, ms))
}

async function main() {
  const words = new Set()
  let done = 0

  for (let start = 1; start <= MAX_ID; start += BATCH) {
    const ids = Array.from({ length: BATCH }, (_, i) => start + i).filter(id => id <= MAX_ID)
    const results = await Promise.allSettled(ids.map(fetchArticle))

    for (const r of results) {
      if (r.status === 'fulfilled') {
        for (const w of r.value) {
          if (VALID.test(w)) words.add(w)
        }
      }
    }

    done += ids.length
    process.stdout.write(`\r${done}/${MAX_ID} articles fetched — ${words.size} words so far`)
    await sleep(DELAY_MS)
  }

  const sorted = [...words].sort()
  writeFileSync(OUT, sorted.join('\n') + '\n', 'utf8')
  console.log(`\n\nDone. ${sorted.length} unique 5-letter words written to ${OUT}`)
}

main().catch(e => { console.error(e); process.exit(1) })
