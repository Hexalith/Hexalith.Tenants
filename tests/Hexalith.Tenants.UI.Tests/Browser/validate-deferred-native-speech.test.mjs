import assert from 'node:assert/strict';
import test from 'node:test';
import { awaitNativeAcknowledgement, requireCompletedNativeTrials, resolveNativeSpeechSelection } from './validate-deferred-native-speech.mjs';

const environment = (group, cultures, trials) => ({
    TENANTS_NATIVE_SPEECH_GROUP: group,
    TENANTS_NATIVE_SPEECH_CULTURES: cultures,
    TENANTS_NATIVE_SPEECH_TRIALS: trials,
});

test('a valid cell cannot hide an excluded culture in the same request', () => {
    assert.throws(() => resolveNativeSpeechSelection(environment('all', 'en', 'S0-en,S0-fr')), /culture is excluded/);
});

test('a valid search cell cannot hide a Create cell excluded by its group', () => {
    assert.throws(() => resolveNativeSpeechSelection(environment('search', 'en', 'S0-en,C1-lifecycle-en')), /excluded by group/);
    assert.throws(() => resolveNativeSpeechSelection(environment('create', 'en', 'C1-lifecycle-en,S3-en')), /excluded by group/);
});

test('empty selector entries and duplicate cultures are rejected', () => {
    assert.throws(() => resolveNativeSpeechSelection(environment('all', '', 'S0-en')), /cultures/);
    assert.throws(() => resolveNativeSpeechSelection(environment('all', 'en,en', 'S0-en')), /Duplicate native cultures/);
    assert.throws(() => resolveNativeSpeechSelection(environment('all', 'en', 'S0-en,')), /Invalid native trial/);
});

test('a selected Refresh requires its fresh paired first insertion and both controls', () => {
    const { requiredTrials } = resolveNativeSpeechSelection(environment('create', 'fr', 'C3-events-availability-fr'));
    assert.deepEqual(requiredTrials, ['C1-availability-fr', 'C3-events-availability-fr']);
    assert.throws(() => requireCompletedNativeTrials(requiredTrials, ['C1-availability-fr']), /coverage is incomplete/);
    assert.doesNotThrow(() => requireCompletedNativeTrials(requiredTrials, [...requiredTrials].reverse()));
});

test('the default plan covers every culture and does not accept a nonempty subset', () => {
    const { requiredTrials } = resolveNativeSpeechSelection({});
    assert.equal(requiredTrials.length, 24);
    assert.throws(() => requireCompletedNativeTrials(requiredTrials, ['S0-en']), /coverage is incomplete/);
    assert.throws(() => requireCompletedNativeTrials(['S0-en'], ['S0-en', 'S0-en']), /Duplicate completed/);
});

test('an alive silent mock collector cannot hold an acknowledgement forever', async () => {
    await assert.rejects(awaitNativeAcknowledgement(new Promise(() => {}), 5), /acknowledgement timed out/);
    assert.deepEqual(await awaitNativeAcknowledgement(Promise.resolve({ done: false, value: 'reply' }), 50), { done: false, value: 'reply' });
});
